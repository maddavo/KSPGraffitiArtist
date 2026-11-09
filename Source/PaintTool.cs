using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KSPGraffitiArtist
{
    [KSPAddon(KSPAddon.Startup.Flight,false)]
    public sealed class PaintTool : MonoBehaviour
    {
        const string LockName="GraffitiArtistEditor";
        const float Reach=3f;
        PaintTarget target,surface;
        Part actor;
        MarkingData draft;
        MarkingData clickedAnchor;
        PaintVisual preview;
        VisibleSurface movementSurface;
        Rect window=new Rect(40,90,390,560);
        Vector2 scroll,selectionScroll;
        Vector2 fontScroll;
        bool showFonts;
        Vector3 mouseDown;
        float downTime,rebuildAt;
        bool pendingClick,changed,textFocused,editing;
        bool editorLockOwned;
        string status="",renderError="";
        string[] images=new string[0];
        string selectedId="";
        static bool eligibleLast;
        void Start()
        {
            Font.textureRebuilt+=FontChanged;
            GameEvents.onCrewBoardVessel.Add(OnBoard);
            GameEvents.onVesselChange.Add(OnVesselChange);
        }
        void OnBoard(GameEvents.FromToAction<Part,Part> transfer)
        { if(actor!=null && transfer.from==actor)Close(); }
        void OnVesselChange(Vessel vessel) { Close();eligibleLast=false; }
        void FontChanged(Font font) { changed=true; }
        bool Equipped(out Part eva)
        {
            eva=null;
            Vessel vessel=FlightGlobals.ActiveVessel;
            if(vessel==null || !vessel.isEVA || vessel.packed || MapView.MapIsEnabled || PauseMenu.isOpen)return false;
            eva=vessel.rootPart;
            if(eva==null)return false;
            foreach(KIS.ModuleKISInventory inventory in eva.FindModulesImplementing<KIS.ModuleKISInventory>())
                foreach(KIS.KIS_Item item in inventory.items.Values)
                    if(item.equipped && item.availablePart!=null && item.availablePart.name=="gaSprayCan")return true;
            return false;
        }
        void Update()
        {
            try { Tick(); }
            catch(Exception ex) { Debug.LogError("[GraffitiArtist] Editor failure: "+ex); Close(); Message("Paint editor closed after an error. See KSP.log."); }
        }
        void Tick()
        {
            Part eva;
            bool eligible=Equipped(out eva);
            if(eligible && !eligibleLast)Message("Spray can equipped: right-click a nearby vessel, KK surface or terrain to paint. Alt + right-click keeps the normal part menu.");
            eligibleLast=eligible;
            if(!eligible) {pendingClick=false;if(draft!=null || editorLockOwned)Close();return;}
            if(draft!=null) {
                if(eva!=actor || !CanReach()) {Close();Message("Painting cancelled: tool or target is no longer in reach.");return;}
                if(Input.GetKeyDown(KeyCode.Escape)) {Close();return;}
                if(!textFocused) {
                    float angle=0,size=0;
                    if(Input.GetKey(KeyCode.Q))angle+=60*Time.unscaledDeltaTime;
                    if(Input.GetKey(KeyCode.E))angle-=60*Time.unscaledDeltaTime;
                    if(Input.GetKey(KeyCode.A))size-=Time.unscaledDeltaTime*.5f;
                    if(Input.GetKey(KeyCode.D))size+=Time.unscaledDeltaTime*.5f;
                    if(angle!=0 || size!=0) {draft.Rotation=MarkingData.Wrapped(draft.Rotation+angle);draft.Width=Mathf.Clamp(draft.Width+size,.1f,4f);changed=true;}
                    float horizontal=0,vertical=0;
                    if(Input.GetKey(KeyCode.J))horizontal-=1;
                    if(Input.GetKey(KeyCode.L))horizontal+=1;
                    if(Input.GetKey(KeyCode.K))vertical-=1;
                    if(Input.GetKey(KeyCode.I))vertical+=1;
                    if(horizontal!=0 || vertical!=0)MoveDraft(horizontal,vertical);
                }
                if(changed && Time.unscaledTime>=rebuildAt)RebuildPreview();
                return;
            }
            if(Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt)) {pendingClick=false;return;}
            if(Input.GetMouseButtonDown(1) && !OverUI()) {mouseDown=Input.mousePosition;downTime=Time.unscaledTime;pendingClick=true;}
            if(pendingClick && (Input.mousePosition-mouseDown).sqrMagnitude>36)pendingClick=false;
            if(Input.GetMouseButtonUp(1)) {
                bool click=pendingClick && Time.unscaledTime-downTime<.4f;pendingClick=false;
                if(click)Open(eva);
            }
        }
        static bool OverUI() { return EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject(); }
        void Open(Part eva)
        {
            Camera camera=FlightCamera.fetch==null?null:FlightCamera.fetch.mainCamera;
            if(camera==null)return;
            var hits=PaintPicking.Hits(camera.ScreenPointToRay(Input.mousePosition));
            foreach(RaycastHit hit in hits) {
                Part part=hit.collider.GetComponentInParent<Part>();
                // The first physical obstruction owns the click; never search through it.
                if(part==eva || (part!=null && (part.vessel==null || part.vessel.isEVA || part.vessel.packed)))return;
                actor=eva;
                if(part!=null){
                    if(part.FindModuleImplementing<ModuleGraffitiSurface>()==null){Message("This part has no Graffiti surface module.");return;}
                    target=new PaintTarget(part);
                }else target=WorldPaintScenario.Instance==null?null:WorldPaintScenario.Instance.Acquire(hit,FlightGlobals.currentMainBody);
                if(target==null){
                    PQ quad=hit.collider.GetComponentInParent<PQ>();
                    Debug.LogWarning("[GraffitiArtist] Rejected target: "+hit.collider.name+", layer "+hit.collider.gameObject.layer+
                        ", parent quad="+(quad!=null)+", world store="+(WorldPaintScenario.Instance!=null));
                    Message("Choose a vessel, a KK instance or solid planetary terrain.");return;
                }
                surface=target;
                Vector3 n=hit.normal.normalized;
                Vector3 u=Vector3.ProjectOnPlane(camera.transform.up,n).normalized;
                if(u.sqrMagnitude<.5f)u=Vector3.ProjectOnPlane(camera.transform.right,n).normalized;
                Vector3 p=target.transform.InverseTransformPoint(hit.point);
                n=target.transform.InverseTransformDirection(n).normalized;u=target.transform.InverseTransformDirection(u).normalized;
                draft=new MarkingData{X=p.x,Y=p.y,Z=p.z,NX=n.x,NY=n.y,NZ=n.z,UX=u.x,UY=u.y,UZ=u.z};
                if(!CanReach()){Close();Message("Move within 3 metres of the surface.");return;}
                // Open the closest existing marking whose projected footprint contains the click.
                clickedAnchor=draft.Copy();
                MarkingData found=surface.Records.Where(m=>Contains(m,p)).OrderBy(m=>(new Vector3(m.X,m.Y,m.Z)-p).sqrMagnitude).FirstOrDefault();
                if(found!=null) {draft=found.Copy();editing=true;selectedId=found.Id;}else{editing=false;selectedId="";}
                surface.Suppress(selectedId);
                images=PaintImages.Names();status="";renderError="";changed=true;textFocused=false;
                InputLockManager.SetControlLock(ControlTypes.ALL_SHIP_CONTROLS|ControlTypes.EVA_INPUT|ControlTypes.TIMEWARP,LockName);
                editorLockOwned=true;
                RebuildPreview();return;
            }
        }
        void MoveDraft(float horizontal,float vertical)
        {
            // Keep movement aligned to the original placement axes, independent of Q/E rotation.
            Vector3 normal=target.transform.TransformDirection(new Vector3(draft.NX,draft.NY,draft.NZ)).normalized;
            Vector3 up=Vector3.ProjectOnPlane(target.transform.TransformDirection(new Vector3(draft.UX,draft.UY,draft.UZ)),normal).normalized;
            Vector3 delta=(VisibleSurface.RightTangent(normal,up)*horizontal+up*vertical).normalized;
            Vector3 current=target.transform.TransformPoint(new Vector3(draft.X,draft.Y,draft.Z));
            if(movementSurface==null)movementSurface=new VisibleSurface(target);
            Vector3 hit;
            // Initial clicks use collision geometry, which may differ from the visible hull.
            if(!movementSurface.Project(current,normal,Mathf.Clamp(draft.Width*.5f,.15f,1.25f),out hit))return;
            Vector3 next=hit+delta*(.25f*Mathf.Min(Time.unscaledDeltaTime,.05f));
            if(!movementSurface.Project(next,normal,.1f,out hit))return;
            Vector3 local=target.transform.InverseTransformPoint(hit);
            if(target.Terrain && local.magnitude>4)return;
            float x=draft.X,y=draft.Y,z=draft.Z;
            draft.X=local.x;draft.Y=local.y;draft.Z=local.z;
            // A rejected step must not close the editor or discard the last valid position.
            if(!CanReach()){draft.X=x;draft.Y=y;draft.Z=z;return;}
            changed=true;
        }
        bool Contains(MarkingData m,Vector3 point)
        {
            Vector3 normal=new Vector3(m.NX,m.NY,m.NZ),up=Quaternion.AngleAxis(-m.Rotation,normal)*new Vector3(m.UX,m.UY,m.UZ);
            Vector3 delta=point-new Vector3(m.X,m.Y,m.Z);
            float height=m.Width;
            if(m.Kind=="image") {Texture2D t=PaintImages.Load(m.Image);if(t!=null)height=m.Width*t.height/t.width;}
            return Mathf.Abs(Vector3.Dot(delta,normal))<Mathf.Max(.15f,m.Width*.5f)
                && Mathf.Abs(Vector3.Dot(delta,VisibleSurface.RightTangent(normal,up)))<m.Width*.55f
                && Mathf.Abs(Vector3.Dot(delta,up))<height*.55f;
        }
        bool CanReach()
        {
            if(target==null || actor==null || !target.Active || draft==null)return false;
            Vector3 point=target.transform.TransformPoint(new Vector3(draft.X,draft.Y,draft.Z));
            Vector3 start=actor.transform.position;Vector3 delta=point-start;
            return delta.sqrMagnitude<=Reach*Reach;
        }
        void RebuildPreview()
        {
            changed=false;rebuildAt=Time.unscaledTime+.15f;
            if(preview!=null){preview.Dispose();preview=null;}
            renderError="";
            if(!draft.Valid()){renderError="Enter text or select an image.";return;}
            try {
                preview=PaintVisual.Create(target,draft);
                if(preview.MissingImage)renderError="Image unavailable or invalid. Choose another PNG.";
                else if(preview.TriangleCount==0)renderError="No visible surface or text. Try a smaller size or different content.";
            } catch(Exception ex){renderError=ex.Message;Debug.LogWarning("[GraffitiArtist] Preview: "+ex.Message);}
        }
        void OnGUI()
        {
            if(draft==null)return;
            GUISkin previousSkin=GUI.skin;
            GUI.skin=HighLogic.Skin;
            window.x=Mathf.Clamp(window.x,0,Mathf.Max(0,Screen.width-390));
            window.y=Mathf.Clamp(window.y,0,Mathf.Max(0,Screen.height-120));
            try { window=GUILayout.Window(140047801,window,DrawWindow,"Graffiti Artist — spray paint",GUILayout.Width(390)); }
            catch(Exception ex){Debug.LogError("[GraffitiArtist] GUI: "+ex);Close();}
            finally { GUI.skin=previousSkin; }
        }
        void DrawWindow(int id)
        {
            GUILayout.Label("I/J/K/L move · Q/E rotate · A/D size");
            GUILayout.Label("I up · K down · J left · L right · Alt + right-click: normal menu");
            GUILayout.Label("Width "+draft.Width.ToString("0.00")+" m   Rotation "+draft.Rotation.ToString("0")+"°");
            scroll=GUILayout.BeginScrollView(scroll,GUILayout.MaxHeight(Mathf.Max(150,Screen.height-260)));
            GUILayout.BeginHorizontal();
            if(GUILayout.Toggle(draft.Kind=="text","Text","Button") && draft.Kind!="text"){draft.Kind="text";GUI.FocusControl(null);changed=true;}
            if(GUILayout.Toggle(draft.Kind=="image","Picture","Button") && draft.Kind!="image"){draft.Kind="image";GUI.FocusControl(null);changed=true;}
            GUILayout.EndHorizontal();
            if(draft.Kind=="text") {
                GUILayout.Label("Text (multiline, up to 512 characters)");GUI.SetNextControlName("ga-text");
                string text=GUILayout.TextArea(draft.Text,512,GUILayout.MinHeight(65));
                if(text!=draft.Text){draft.Text=text;changed=true;}
                if(GUILayout.Button("Font: "+draft.Font)){showFonts=!showFonts;if(showFonts)PaintFonts.Refresh();GUI.FocusControl(null);}
                if(!PaintFonts.Available(draft.Font))GUILayout.Label("Font unavailable here; displaying Arial. The saved font name is retained.");
                if(showFonts){
                    fontScroll=GUILayout.BeginScrollView(fontScroll,GUILayout.Height(120));
                    foreach(string name in PaintFonts.Names)if(GUILayout.Button(name)){
                        draft.Font=name;changed=true;showFonts=false;GUI.FocusControl(null);
                    }
                    GUILayout.EndScrollView();
                }
                string[] styles={"Regular","Bold","Italic","BoldItalic"};
                int style=Array.IndexOf(styles,draft.Style);
                int selected=GUILayout.SelectionGrid(style,new[]{"Regular","Bold","Italic","Bold Italic"},2);
                if(selected!=style && selected>=0){draft.Style=styles[selected];GUI.FocusControl(null);changed=true;}
                draft.R=ColourSlider("Red",draft.R);draft.G=ColourSlider("Green",draft.G);draft.B=ColourSlider("Blue",draft.B);
            } else {
                GUILayout.Label("PNG pictures: GameData/KSPGraffitiArtist/Images");
                GUILayout.Label("Selected: "+draft.Image);
                if(GUILayout.Button("Refresh pictures")){PaintImages.Clear();images=PaintImages.Names();foreach(var s in FindObjectsOfType<ModuleGraffitiSurface>())s.Invalidate();if(WorldPaintScenario.Instance!=null)WorldPaintScenario.Instance.InvalidateAll();changed=true;}
                selectionScroll=GUILayout.BeginScrollView(selectionScroll,GUILayout.Height(155));
                for(int i=0;i<images.Length;i++) {
                    Texture2D t=null;try{t=PaintImages.Load(images[i]);}catch(Exception ex){Debug.LogWarning("[GraffitiArtist] Image: "+ex.Message);}
                    GUILayout.BeginHorizontal();
                    if(t!=null)GUILayout.Label(t,GUILayout.Width(48),GUILayout.Height(40));
                    if(GUILayout.Button(images[i])){draft.Image=images[i];changed=true;}
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.Label("Choose marking to edit (selecting discards the current draft):");
            if(GUILayout.Button("New marking at clicked point")){draft=clickedAnchor.Copy();draft.Id=Guid.NewGuid().ToString("N");editing=false;selectedId="";surface.Suppress(null);changed=true;}
            foreach(MarkingData record in surface.Records) {
                string label=record.Kind=="text"?record.Text.Replace('\n',' '):record.Image;
                if(label.Length>32)label=label.Substring(0,32)+"…";
                if(GUILayout.Button((record.Id==selectedId?"● ":"")+label)) {draft=record.Copy();editing=true;selectedId=record.Id;surface.Suppress(selectedId);changed=true;}
            }
            GUILayout.EndScrollView();
            if(renderError!="")GUILayout.Label(renderError);
            if(status!="")GUILayout.Label(status);
            textFocused=GUI.GetNameOfFocusedControl()=="ga-text";
            GUILayout.BeginHorizontal();
            bool previous=GUI.enabled;
            GUI.enabled=preview!=null && !changed && renderError=="" && draft.Valid();
            if(GUILayout.Button("Apply")) {
                Part eva;
                if(Equipped(out eva) && eva==actor && CanReach() && surface.Commit(draft)){Close();GUILayout.EndHorizontal();return;}
                status="Cannot apply: target/tool changed or this surface already has 32 markings.";
            }
            GUI.enabled=previous;
            if(editing && GUILayout.Button("Erase")) {
                Part eva;
                if(Equipped(out eva) && eva==actor && CanReach()){surface.Erase(draft.Id);Close();GUILayout.EndHorizontal();return;}
                status="Cannot erase: target or tool changed.";
            }
            if(GUILayout.Button("Cancel")){Close();GUILayout.EndHorizontal();return;}
            GUILayout.EndHorizontal();GUI.DragWindow(new Rect(0,0,390,24));
        }
        float ColourSlider(string name,float value)
        {
            GUILayout.BeginHorizontal();GUILayout.Label(name,GUILayout.Width(55));
            float next=GUILayout.HorizontalSlider(value,0,1);GUILayout.EndHorizontal();
            if(Mathf.Abs(next-value)>.001f)changed=true;return next;
        }
        static void Message(string text){ScreenMessages.PostScreenMessage(text,5,ScreenMessageStyle.UPPER_CENTER);}
        void LateUpdate()
        {
            if(draft==null)return;
            Part eva;
            if(!Equipped(out eva) || eva!=actor){Close();return;}
            if(Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt))return;
            PawIntegration.DismissForPaint(UIPartActionController.Instance,target.Part);
        }
        void Close()
        {
            InputLockManager.RemoveControlLock(LockName);
            editorLockOwned=false;
            if(surface!=null)surface.Suppress(null);
            if(preview!=null){preview.Dispose();preview=null;}
            movementSurface=null;
            showFonts=false;
            draft=null;target=null;surface=null;actor=null;textFocused=false;editing=false;pendingClick=false;
        }
        void OnDisable(){Close();}
        void OnDestroy()
        {
            Close();Font.textureRebuilt-=FontChanged;
            GameEvents.onCrewBoardVessel.Remove(OnBoard);
            GameEvents.onVesselChange.Remove(OnVesselChange);
            PaintImages.Clear();FontCoverageAtlas.Clear();PaintFonts.Clear();eligibleLast=false;
        }
    }
}
