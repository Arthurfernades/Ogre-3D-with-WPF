using org.ogre;
using SharpDX.Direct3D11;
using System;
using System.Windows.Interop;
using System.Windows;

namespace SharpDXLib
{

    public class OgreRender
    {
        private Root root;
        private IntPtr _sharedHandle;
        private Texture2D _sharedTexture;
        private RenderSystem renderSystem;

        public IntPtr SharedHandle => _sharedHandle;

        public void Initialize(int width, int height)
        {
            // Inicialize o Ogre
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

                if (rs.getName() == "Direct3D11 Rendering Subsystem") // Se mudar para o Direct3D11 tem que ativar o ShaderGenerator
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
            //renderSystem.setConfigOption("Floating-point mode", "Consistent");
            //renderSystem.setConfigOption("Resource Creation Policy", "Create on active device");
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
                ["FSAA"] = "0",// Level 4 anti aliasing 
                ["Full Screen"] = "No",
                ["Video Mode"] = "800 x 600",
                ["Allow NVPerfHUD"] = "No",
                ["Floating-point mode"] = "Consistent",
                ["Resource Creation Policy"] = "Create on active device",
                ["VSync"] = "No",
                ["VSync Interval"] = "1",
                ["sRGB Gamma Conversion"] = "No",
                ["externalWindowHandle"] = hWnd.ToString()
            };

            RenderWindow renderWindow = root.createRenderWindow(
                "Window Forms Ogre", //Render Targer name
                0, // Width
                0, // Height
                false, // Windowed mode
                miscParams
                );

            renderWindow.setAutoUpdated(false);

            #endregion

            SceneManager scnMgr = root.createSceneManager();

            var shadergen = ShaderGenerator.getSingleton(); //Usado somente com DX11
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

            var vp = renderWindow.addViewport(cam);
            vp.setBackgroundColour(new ColourValue(.3f, .3f, .3f));

            #endregion

            #region Entity

            var ent = scnMgr.createEntity("Sinbad.mesh");
            var node = scnMgr.getRootSceneNode().createChildSceneNode();
            node.attachObject(ent);

            #endregion

            // Configurar a textura compartilhada no Ogre3D
            _sharedHandle = CreateSharedTexture(width, height);
        }

        private IntPtr CreateSharedTexture(int width, int height)
        {
            // Configuração do lado do Ogre3D para criar uma textura compartilhada
            var texture = TextureManager.getSingleton().createManual(
                "SharedTexture",
                ResourceGroupManager.DEFAULT_RESOURCE_GROUP_NAME,
                TextureType.TEX_TYPE_2D,
                800,
                600,
                32,
                0,
                PixelFormat.PF_R8G8B8A8,
                (int)0x20);

            // Retorne o handle da textura compartilhada
            return new IntPtr(texture.getHandle());
        }

        public void RenderFrame()
        {
            root.renderOneFrame();
        }
    }

}
