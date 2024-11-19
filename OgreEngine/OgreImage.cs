using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using org.ogre;

namespace OgreEngine
{
    public partial class OgreImage : D3DImage, ISupportInitialize
    {

        private delegate void MethodInvoker();

        private Root root;
        private TexturePtr texture;
        private RenderWindow renderWindow;
        private org.ogre.Camera camera;
        private Viewport viewport;
        private SceneManager sceneManager;
        private RenderTarget renTarget;
        private int reloadRenderTargetTime;
        private bool imageSourceValid;
        private Thread currentThread;
        private DispatcherTimer timer;
        private bool eventAttatched;

        public Root Root
        {
            get { return root; }
        }

        public SceneManager SceneManager
        {
            get { return sceneManager; }
        }

        public RenderWindow RenderWindow
        {
            get { return renderWindow; }
        }

        public RenderTarget RenderTarget
        {
            get { return renTarget; }
        }

        public TexturePtr Texture
        {
            get { return texture; }
        }

        public org.ogre.Camera Camera
        {
            get { return camera; }
        }

        public event RoutedEventHandler Initialised;

        public event EventHandler PreRender;

        public event EventHandler PostRender;

        public void Dispose()
        {
            IsFrontBufferAvailableChanged -= _isFrontBufferAvailableChanged;

            DetachRenderTarget(true, true);

            if (currentThread != null)
            {
                currentThread.Abort();
            }

            if (root != null)
            {
                DisposeRenderTarget();
                CompositorManager.getSingleton().removeAll();

                root.Dispose();
                root = null;
            }

            GC.SuppressFinalize(this);
        }

        public void BeginInit()
        {
        }

        public void EndInit()
        {
            if (AutoInitialise)
            {
                InitOgre();
            }
        }

        public bool InitOgre()
        {
            return true;//_InitOgre();
        }

        public Thread InitOgreAsync(ThreadPriority priority, RoutedEventHandler completeHandler)
        {
            if (completeHandler != null)
                Initialised += completeHandler;

            currentThread = new Thread(() => _InitOgre())
            {
                Priority = priority
            };
            currentThread.Start();

            return currentThread;
        }

        public void InitOgreAsync()
        {
            InitOgreAsync(ThreadPriority.Normal, null);
        }

        public void _InitOgre()
        {
            lock (this)
            {
                // Get Windows Handle
                //
                IntPtr hWnd = GetWindowHandle();

                // Load the OGRE engine
                //
                root = new Root();

                // Configure resource paths from : "resources.cfg" file
                //
                ConfigurePaths();

                // Configures the application and creates the Window a window HAS to be created, even though we'll never use it.
                //
                //if (!FindRenderSystem())
                //    continue;

                FindRenderSystem();

                RenderSystemConfig();

                root.initialise(false);

                RenderWindowConfig(hWnd);

                ResourceGroupManager.getSingleton().initialiseAllResourceGroups();

                this.Dispatcher.Invoke(
                    (MethodInvoker)delegate
                    {
                        if (!CreateDefaultScene)
                        {
                            DefaultScene();
                        }

                        IsFrontBufferAvailableChanged += _isFrontBufferAvailableChanged;

                        if (Initialised != null)
                            Initialised(this, new RoutedEventArgs());

                        ReInitRenderTarget();

                        AttachRenderTarget(true);

                        //OnFrameRateChanged(this.FrameRate);

                        currentThread = null;
                    });
                //return true;
            }
        }

        private void DefaultScene()
        {
            sceneManager = root.createSceneManager();
            sceneManager.setAmbientLight(new ColourValue(0.5f, 0.5f, 0.5f));

            SceneNode camNode = sceneManager.getRootSceneNode().createChildSceneNode();

            camera = sceneManager.createCamera("DefaultCamera");
            camNode.setPosition(200, 300, 400);
            camNode.lookAt(new Vector3(0, 0, 0), Node.TransformSpace.TS_WORLD);
            camera.setNearClipDistance(5);
            camNode.attachObject(camera);

            Viewport vp = renderWindow.addViewport(camera);
            vp.setBackgroundColour(new ColourValue(0, 0, 0));

            camera.setAspectRatio(vp.getActualWidth() / vp.getActualHeight());
        }

        private void RenderWindowConfig(IntPtr hWnd)
        {
            var misc = new NameValueMap();
            misc["externalWindowHandle"] = hWnd.ToString();
            renderWindow = root.createRenderWindow("OgreImageSource Windows", 0, 0, false, misc);
            renderWindow.setAutoUpdated(false);
        }

        private void RenderSystemConfig()
        {
            root.getRenderSystem().setConfigOption("Full Screen", "No");
            root.getRenderSystem().setConfigOption("Video Mode", "640 x 480 @ 32-bit colour");
        }

        private bool FindRenderSystem()
        {
            foreach (RenderSystem rs in root.getAvailableRenderers())
            {
                if (rs == null) continue;

                root.setRenderSystem(rs);

                if (root.getRenderSystem().getName() == "Direct3D11 Rendering Subsystem")
                {
                    foreach (var c in rs.getConfigOptions())
                    {
                        foreach (var p in c.Value.possibleValues)
                        {
                            LogManager.getSingleton().logMessage(p);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private static void ConfigurePaths()
        {
            var configFile = new ConfigFile();
            configFile.loadDirect("resources.cfg", "\t:=", true);

            // Go through all sections
            //
            var seci = configFile.getSettingsBySection();

            foreach (var section in seci)
            {
                if (section.Key != null && section.Key != "")
                {
                    string sectionName = section.Key;

                    var settings = configFile.getMultiSetting("Zip", sectionName);

                    foreach (var setting in settings)
                    {
                        ResourceGroupManager.getSingleton().addResourceLocation(setting, "Zip", sectionName);
                    }

                    settings = configFile.getMultiSetting("FileSystem", sectionName);

                    foreach (var setting in settings)
                    {
                        ResourceGroupManager.getSingleton().addResourceLocation(setting, "FileSystem", sectionName);
                    }
                }
            }
        }

        private IntPtr GetWindowHandle()
        {
            IntPtr hWnd = IntPtr.Zero;

            foreach (PresentationSource source in PresentationSource.CurrentSources)
            {
                var hwndSource = (source as HwndSource);

                if (hwndSource != null)
                {
                    hWnd = hwndSource.Handle;
                    break;
                }
            }
            return hWnd;
        }

        protected void RenderFrame()
        {
            if ((camera != null) && (viewport == null))
            {
                viewport = renTarget.addViewport(camera);
                viewport.setBackgroundColour(new ColourValue(0.0f, 0.0f, 0.0f, 0.0f));
            }

            if (PreRender != null)
                PreRender(this, EventArgs.Empty);

            root.renderOneFrame();

            if (PostRender != null)
                PostRender(this, EventArgs.Empty);
        }

        protected void DisposeRenderTarget()
        {
            if (renTarget != null)
            {
                CompositorManager.getSingleton().removeCompositorChain(viewport);
                renTarget.removeAllListeners();
                renTarget.removeAllViewports();
                root.getRenderSystem().destroyRenderTarget(renTarget.getName());
                renTarget = null;
                viewport = null;
            }

            if (texture != null)
            {
                TextureManager.getSingleton().remove(texture.getHandle());
                texture.Dispose();
                texture = null;
            }
        }

        protected void ReInitRenderTarget()
        {
            DetachRenderTarget(true, false);
            DisposeRenderTarget();

            texture = TextureManager.getSingleton().createManual(
                "OgreImageSource RenderTarget",
                ResourceGroupManager.DEFAULT_RESOURCE_GROUP_NAME,
                TextureType.TEX_TYPE_2D,
                (uint)ViewportSize.Width,
                (uint)ViewportSize.Height,
                32,
                0,
                org.ogre.PixelFormat.PF_R8G8B8A8,
                (int)TextureUsage.TU_RENDERTARGET
            );
            renTarget = texture.getBuffer().getRenderTarget();
            reloadRenderTargetTime = 0;
        }

        protected virtual void AttachRenderTarget(bool attachEvent)
        {
            if (!imageSourceValid)
            {

                try
                {
                    unsafe
                    {
                        IntPtr surface = IntPtr.Zero;

                        byte[] buffer = new byte[sizeof(IntPtr)];
                        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                        root.getRenderSystem().getCustomAttribute("D3DDEVICE", handle.AddrOfPinnedObject());

                        //renderWindow.getCustomAttribute("D3DDEVICE", handle.AddrOfPinnedObject());

                        //renderWindow.getCustomAttribute("WINDOW", handle.AddrOfPinnedObject());
                        
                        //renTarget.getCustomAttribute("DDBACKBUFFER", surface);

                        surface = (IntPtr) BitConverter.ToInt64(buffer,0);

                        //1 - criar uma textura directx9
                        //2 - compartilhar o handle do dx9 com o dx11 do ogro
                        //3 - chamar o setbackbuffer com a textura do 9

                        Lock();
                        SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface);
                        Unlock();

                        imageSourceValid = true;
                    }
                }
                finally
                {
                }
            }

            if (attachEvent)
                UpdateEvents(true);
        }

        protected virtual void DetachRenderTarget(bool detatchSurface, bool detatchEvent)
        {
            if (detatchSurface && imageSourceValid)
            {
                Lock();
                SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                Unlock();

                imageSourceValid = false;
            }

            if (detatchEvent)
                UpdateEvents(false);
        }

        protected virtual void UpdateEvents(bool attach)
        {
            eventAttatched = attach;
            if (attach)
            {
                if (timer != null)
                    timer.Tick += _rendering;
                else
                    CompositionTarget.Rendering += _rendering;
            }
            else
            {
                if (timer != null)
                    timer.Tick -= _rendering;
                else
                    CompositionTarget.Rendering -= _rendering;
            }
        }

        private void _isFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsFrontBufferAvailable)
                AttachRenderTarget(true); // might not succeed
            else
                // need to keep old surface attached because it's the only way to get the front buffer active event.
                DetachRenderTarget(false, true);
        }

        private void _rendering(object sender, EventArgs e)
        {
            if (root == null) return;

            if (IsFrontBufferAvailable)
            {
                /*if (MogreWpf.Interop.D3D9RenderSystem.IsDeviceLost(_renderWindow))
                {
                    _renderWindow.update(); // try restore
                    _reloadRenderTargetTime = -1;

                    if (MogreWpf.Interop.D3D9RenderSystem.IsDeviceLost(_renderWindow))
                        return;
                }*/

                long durationTicks = ResizeRenderTargetDelay.TimeSpan.Ticks;

                // if the new next ReInitRenderTarget() interval is up
                if (((reloadRenderTargetTime < 0) || (durationTicks <= 0))
                    // negative time will be reloaded immediatly
                    ||
                    ((reloadRenderTargetTime > 0) &&
                     (Environment.TickCount >= (reloadRenderTargetTime + durationTicks))))
                {
                    ReInitRenderTarget();
                }

                if (!imageSourceValid)
                    AttachRenderTarget(false);

                Lock();
                RenderFrame();
                AddDirtyRect(new Int32Rect(0, 0, PixelWidth, PixelHeight));
                Unlock();
            }
        }

        private void OnFrameRateChanged(int? newFrameRate)
        {
            bool wasAttached = eventAttatched;
            UpdateEvents(false);

            if (newFrameRate == null)
            {
                if (timer != null)
                {
                    timer.Tick -= _rendering;
                    timer.Stop();
                    timer = null;
                }
            }
            else
            {
                if (timer == null)
                    timer = new DispatcherTimer();

                timer.Interval = new TimeSpan(1000 / newFrameRate.Value);
                timer.Start();
            }

            if (wasAttached)
                UpdateEvents(true);
        }
    }
}
