using org.ogre;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace OgreEngine
{
    public partial class OgreImage : D3DImage
    {
        private static Root root;

        RenderWindow renderWindow;

        private RenderTarget renderTarget;

        private RenderSystem renderSystem;

        private RenderTexture renderTexture;

        private TexturePtr texturePtr;

        private Camera cam;

        private DispatcherTimer timer;

        #region ViewportSize Property

        public static readonly DependencyProperty ViewportSizeProperty =
            DependencyProperty.Register("ViewportSize", typeof(Size), typeof(OgreImage),
                                        new PropertyMetadata(new Size(100, 100), OnViewportProperyChanged)
                );

        private static void OnViewportProperyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var imageSource = (OgreImage)d;
        }

        public Size ViewportSize
        {
            get { return (Size)GetValue(ViewportSizeProperty); }
            set { SetValue(ViewportSizeProperty, value); }
        }

        #endregion

        private Viewport vp;


        private bool _imageSourceValid;

        public void Initialize()
        {
            root = new Root();

            #region Log

            //LogManager logMngr = new LogManager();
            //Log log = LogManager.getSingleton().createLog("OgreLog.log", true, true, false);

            #endregion

            #region Puglins / Resources config


            ConfigFile configFile = new ConfigFile();

            configFile.loadDirect("resources.cfg", "\t:=", true);

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

            #endregion

            #region RenderSystem

            bool foundit = false;

            foreach (RenderSystem rs in root.getAvailableRenderers())
            {
                if (rs == null) continue;

                if (rs.getName() == "Direct3D9 Rendering Subsystem") // Se mudar para o Direct3D11 tem que ativar o ShaderGenerator
                {
                    root.setRenderSystem(rs);

                    renderSystem = rs;

                    foreach (var c in rs.getConfigOptions())
                    {
                        foreach (var p in c.Value.possibleValues)
                        {
                            LogManager.getSingleton().logMessage(p);
                        }
                    }

                    foundit = true;
                    break;
                }
            }

            if (!foundit)
            {
                throw new Exception("Failed to find a compatible render system.");
            }

            renderSystem.setConfigOption("Full Screen", "No");
            renderSystem.setConfigOption("Video Mode", "800 x 600 @ 32-bit colour");
            renderSystem.setConfigOption("Allow NVPerfHUD", "No");
            renderSystem.setConfigOption("FSAA", "0");
            renderSystem.setConfigOption("Floating-point mode", "Consistent");
            renderSystem.setConfigOption("Resource Creation Policy", "Create on active device");
            renderSystem.setConfigOption("VSync", "No");

            #endregion

            root.initialise(false);

            #region Render Window

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

            NameValueMap miscParams = new NameValueMap
            {
                ["FSAA"] = "0",
                ["Full Screen"] = "No",
                ["VSync"] = "No",
                ["sRGB Gamma Conversion"] = "No",
                ["externalWindowHandle"] = hWnd.ToString()
            };

            renderWindow = root.createRenderWindow(
                "Window Forms Ogre", //Render Targer name
                1, // Width
                1, // Height
                false, // Windowed mode
                miscParams
                );

            renderWindow.setAutoUpdated(false);

            #endregion

            SceneManager scnMgr = root.createSceneManager();

            //var shadergen = ShaderGenerator.getSingleton(); //Usado somente com DX11
            //shadergen.addSceneManager(scnMgr);

            #region Ambient Light

            scnMgr.setAmbientLight(new ColourValue(.1f, .1f, .1f));

            #endregion

            #region Light

            var light = scnMgr.createLight("MainLight");
            var lightnode = scnMgr.getRootSceneNode().createChildSceneNode();
            lightnode.setPosition(0f, 10f, 15f);
            lightnode.attachObject(light);

            #endregion

            #region Camera

            var cam = scnMgr.createCamera("myCam");
            cam.setAutoAspectRatio(true);
            cam.setNearClipDistance(5);
            var camnode = scnMgr.getRootSceneNode().createChildSceneNode();
            camnode.attachObject(cam);

            #endregion

            #region Camera Man

            var camman = new CameraMan(camnode);
            camman.setStyle(CameraStyle.CS_ORBIT);
            camman.setYawPitchDist(new Radian(0), new Radian(0.3f), 15f);

            #endregion

            #region Viewport

            vp = renderWindow.addViewport(cam);
            vp.setBackgroundColour(new ColourValue(.3f, .3f, .3f));

            #endregion

            #region Entity

            var ent = scnMgr.createEntity("Sinbad.mesh");
            var node = scnMgr.getRootSceneNode().createChildSceneNode();
            node.attachObject(ent);

            #endregion

            IsFrontBufferAvailableChanged += _isFrontBufferAvailableChanged;
        }

        public void InitRenderTarget()
        {
            DetachRenderTarget(true, false);
            DisposeRenderTarget();

            texturePtr = TextureManager.getSingleton().createManual(
                "SharedTexture",
                ResourceGroupManager.DEFAULT_RESOURCE_GROUP_NAME,
                TextureType.TEX_TYPE_2D,
                (uint)ViewportSize.Width,
                (uint)ViewportSize.Height,
                64,
                0,
                org.ogre.PixelFormat.PF_R8G8B8A8,
                (int)TextureUsage.TU_RENDERTARGET);

            renderTarget = texturePtr.getBuffer().getRenderTarget();
            renderTarget.addViewport(cam);
        }

        protected void DisposeRenderTarget()
        {
            if (renderTarget != null)
            {
                renderTarget.removeAllListeners();
                renderTarget.removeAllViewports();
                renderSystem.destroyRenderTarget(renderTarget.getName());
                renderTarget = null;
            }

            if (texturePtr != null)
            {
                TextureManager.getSingleton().remove(texturePtr.getHandle());
                texturePtr.Dispose();
                texturePtr = null;
            }
        }



        public virtual unsafe void AttachRenderTarget()
        {
            try
            {
                /*
                var surface = IntPtr.Zero;

                byte[] buffer = new byte[sizeof(IntPtr)];

                GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                renderTarget.getCustomAttribute("DDBACKBUFFER", handle.AddrOfPinnedObject());
                surface = (IntPtr)BitConverter.ToInt64(buffer, 0);
                */

                uint p = renderTarget.getCustomAttribute("DDBACKBUFFER");
                var surface = new IntPtr(p);


                Lock();
                SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface, true);
            }
            finally
            {
                Unlock();
            }
        }

        private void _isFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsFrontBufferAvailable)
                AttachRenderTarget();
            else
            {
                DetachRenderTarget(false, true);
                _imageSourceValid = false;
            }
        }

        protected virtual void DetachRenderTarget(bool detatchSurface, bool detatchEvent)
        {
            if (detatchSurface && _imageSourceValid)
            {
                Lock();
                SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                Unlock();

                _imageSourceValid = false;
            }
        }


        public void RenderLoop()
        {

            //System.Windows.Forms.Application.DoEvents();

            while (!root.endRenderingQueued())
            {
                Lock();

                root.renderOneFrame();

                AddDirtyRect(new Int32Rect(0, 0, PixelWidth, PixelHeight));

                Unlock();

                Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
            }
        }

        public void Dispose()
        {
            root.queueEndRendering();
            CompositorManager.getSingleton().removeAll();
            GC.Collect();
        }
    }
}