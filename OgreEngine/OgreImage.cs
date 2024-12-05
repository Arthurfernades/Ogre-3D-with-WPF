using org.ogre;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Xml.Linq;
using Math = System.Math;

namespace OgreEngine
{
    public partial class OgreImage : D3DImage
    {
        public Root root;

        private RenderWindow renderWindow;

        private RenderTarget renderTarget;

        private RenderSystem renderSystem;

        private RenderTexture renderTexture;

        private TexturePtr texturePtr;

        private SceneManager scnMgr;

        private Camera cam;

        private DispatcherTimer renderTimer;

        private CameraMan camman;

        private float distance, xAxis, yAxis;

        private float xAxisEntity, yAxisEntity, zAxisEntity;

        private float xAxisEntityDirection, yAxisEntityDirection, zAxisEntityDirection;

        private int xRotationCount, yRotationCount, zRotationCount;

        private Radian yaw;

        private Radian pitch;

        private SceneNode entityNode, camnode;

        private Entity entity;

        public OgreImage()
        {
            #region Camera Man initial config

            distance = 150;
            xAxis = 0;
            yAxis = 0;

            yaw = new Radian(xAxis);
            pitch = new Radian(yAxis);

            #endregion

            #region Entity position

            xAxisEntity = 1;
            yAxisEntity = 0;
            zAxisEntity = 0;

            xAxisEntityDirection = 0;
            yAxisEntityDirection = 0;
            zAxisEntityDirection = 0;

            #endregion
        }

        public void setCameraDistance(bool approaching)
        {
            if (approaching)
            {
                distance -= 0.2f;

            } else
            {
                distance += 0.2f;
            }

            camman.setYawPitchDist(new Radian(xAxis), new Radian(yAxis), distance);
        }

        public static class MathUtils
        {
            public static double Clamp(double value, double min, double max)
            {
                if (value < min) return min;
                if (value > max) return max;
                return value;
            }
        }

        public void setCameraAngle(bool xMove, bool yMove, bool xGrow, bool yGrow)
        {            
            float camSense = 0.02f;
            float diagonalSense = 2f * (float)Math.Sqrt(3);

            if (xMove && yMove)
            {
                xAxis += (xGrow ? 1 : -1) * camSense * diagonalSense;
                yAxis += (yGrow ? -1 : 1) * camSense * diagonalSense;
            }
            else if (xMove)
            {
                xAxis += (xGrow ? 1 : -1) * camSense;
            }
            else if (yMove)
            {
                yAxis += (yGrow ? -1 : 1) * camSense * 5;
            }

            xAxis = (float)MathUtils.Clamp(xAxis, -Math.PI, Math.PI);
            yAxis = (float)MathUtils.Clamp(yAxis, -Math.PI / 2, Math.PI / 2);

            camman.setYawPitchDist(new Radian(xAxis), new Radian(yAxis), distance);
        }


        public void setEntityPostiton(bool grow, string axis)
        {
            float sense = 1;

            if (axis == "x")
            {
                if(!grow)
                    xAxisEntity += sense;
                else
                    xAxisEntity -= sense;

            }

            if (axis == "y")
            {
                if (grow)
                    yAxisEntity += sense;
                else
                    yAxisEntity -= sense;

            }

            if (axis == "z")
            {
                if (grow)
                    zAxisEntity += sense;
                else
                    zAxisEntity -= sense;

            }

            entityNode.setPosition(xAxisEntity, yAxisEntity, zAxisEntity);
            camman.setYawPitchDist(new Radian(xAxis), new Radian(yAxis), distance);

        }

        public void setEntityLookDirection(bool grow, string axis)
        {
            float sense = 1f;

            if (axis == "x")
            {
                if (grow)
                {                                
                    yAxisEntityDirection += sense;
                    yAxisEntity += sense;
                    entityNode.setDirection(0, yAxisEntity, 0, Node.TransformSpace.TS_LOCAL, new Vector3(0, 0, -1));
                    xRotationCount++;
                 
                } else
                {
                    yAxisEntityDirection -= sense;
                    yAxisEntity -= sense;
                    entityNode.setDirection(0, yAxisEntity, 0, Node.TransformSpace.TS_LOCAL, new Vector3(0, 0, -1));
                }

            } else if (axis == "z")
            {
                if (grow)
                {
                    //zAxisEntityDirection += sense;
                    xAxisEntity += sense;
                    zAxisEntity += sense;
                    entityNode.setDirection(xAxisEntity, 0, zAxisEntity, Node.TransformSpace.TS_LOCAL, new Vector3(0, 0, -1));
                }
                else
                {
                    //zAxisEntityDirection -= sense;
                    xAxisEntity -= sense;
                    zAxisEntity -= sense;
                    entityNode.setDirection(xAxisEntity, 0, zAxisEntity, Node.TransformSpace.TS_LOCAL, new Vector3(0, 0, -1));
                }
            }

        }

        #region ViewportSize Property

        public static readonly DependencyProperty ViewportSizeProperty =
            DependencyProperty.Register("ViewportSize", typeof(Size), typeof(OgreImage),
                                        new PropertyMetadata(new Size(1920, 1080), OnViewportProperyChanged)
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


        private bool isImageSourceValid;

        public bool isInited = false;

        public void Initialize(bool createDefaultScene)
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
                if (rs           == null) continue;

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
            renderSystem.setConfigOption("Video Mode", "1920 x 1080 @ 32-bit colour");
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
                ["FSAA"] = "0",
                ["Full Screen"] = "No",
                ["VSync"] = "No",
                ["sRGB Gamma Conversion"] = "No",
                ["externalWindowHandle"] = hWnd.ToString()
            };

            renderWindow = root.createRenderWindow(
                "Window Forms Ogre", //Render Targer name
                0, // Width
                0, // Height
                false, // Windowed mode
                miscParams
                );

            renderWindow.setAutoUpdated(false);

            #endregion

            ResourceGroupManager.getSingleton().initialiseAllResourceGroups();                          

            //ResourceGroupManager.getSingleton()

            #region Create Scene

            scnMgr = root.createSceneManager();

            if (createDefaultScene)
            {

                #region Shader Generator (DX11)

                //Usado somente com DX11
                //var shadergen = ShaderGenerator.getSingleton();
                //shadergen.addSceneManager(scnMgr);

                #endregion

                #region Shadow

                /*scnMgr.setShadowTechnique(ShadowTechnique.SHADOWTYPE_TEXTURE_MODULATIVE_INTEGRATED);
                scnMgr.setShadowTexturePixelFormat(org.ogre.PixelFormat.PF_DEPTH16);
                scnMgr.setShadowColour(new ColourValue(0.5f, 0.5f, 0.5f));
                scnMgr.setShadowTextureSize(1024);
                scnMgr.setShadowTextureCount(1);
                scnMgr.setShadowDirLightTextureOffset(0);
                scnMgr.setShadowFarDistance(50);
                scnMgr.setShadowCameraSetup(LiSPSMShadowCameraSetup.create());*/

                #endregion

                #region Ambient Light

                scnMgr.setAmbientLight(new ColourValue(0f, 0f, 0f));

                #endregion

                entity = scnMgr.createEntity("ogrehead.mesh");
                entityNode = scnMgr.getRootSceneNode().createChildSceneNode();
                entityNode.setPosition(xAxisEntity, yAxisEntity, zAxisEntity);
                entityNode.attachObject(entity);
                entityNode.showBoundingBox(true);

                #region Camera

                cam = scnMgr.createCamera("myCam");
                cam.setAutoAspectRatio(true);
                cam.setNearClipDistance(5);
                camnode = scnMgr.getRootSceneNode().createChildSceneNode();
                camnode.attachObject(cam);
                camnode.setAutoTracking(true, entityNode);
                #endregion


                #region Camera Man

                camman = new CameraMan(camnode);
                camman.setStyle(CameraStyle.CS_MANUAL);
                camman.setTarget(entityNode);
                camman.setYawPitchDist(yaw, pitch, distance);

                #endregion

                #region Viewport

                //só usar se for renderizar direto na renderwindow
                //vp = renderWindow.addViewport(cam);
                //vp.setBackgroundColour(new ColourValue(1f, 1f, 1f, 1));

                #endregion

            }

            #endregion

            isInited = true;

            IsFrontBufferAvailableChanged += _isFrontBufferAvailableChanged;
        }

        public void CreateSceneDefault()
        {

            #region Light

            var light = scnMgr.createLight("MainLight");
            var lightnode = scnMgr.getRootSceneNode().createChildSceneNode();
            lightnode.setPosition(10f, 10f, 10f);
            lightnode.attachObject(light);

            #endregion       
        }

        public void ChangeEntity(string model3D)
        {
            scnMgr.destroyEntity(entity);
            Entity newEntity = entity = scnMgr.createEntity(model3D + ".mesh");
            entity = newEntity;
            entityNode.attachObject(entity);
        }

        public void AddEntity(string model3D)
        {
            Entity newEntity = scnMgr.createEntity(model3D + ".mesh");
            SceneNode newEntityNode = scnMgr.getRootSceneNode().createChildSceneNode();
            newEntityNode.setPosition(xAxisEntity, yAxisEntity, zAxisEntity);
            newEntityNode.attachObject(newEntity);
        }

        public void DetectColision()
        {
        }

        public void InitRenderTarget()
        {
            DetachRenderTarget(true, false);
            DisposeRenderTarget();

            texturePtr = TextureManager.getSingleton().createManual(
                        "Ogre Render",
                        ResourceGroupManager.DEFAULT_RESOURCE_GROUP_NAME,
                        TextureType.TEX_TYPE_2D,
                        (uint)ViewportSize.Width,
                        (uint)ViewportSize.Height,
                        32,
                        0,
                        PixelFormat.PF_R8G8B8A8,
                        (int)TextureUsage.TU_RENDERTARGET); // | (int)TextureUsage.TU_SHARED_RESOURCE

            renderTarget = texturePtr.getBuffer().getRenderTarget();

            // Esse é o código pra deixar o fundo transparente
            renderTarget.removeAllViewports();
            vp = renderTarget.addViewport(cam);
            vp.setBackgroundColour(new ColourValue(0f, 0f, 0f, 0f));
            vp.setClearEveryFrame(true);
            vp.setOverlaysEnabled(false);
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
                GC.SuppressFinalize(texturePtr);
                //texturePtr.Dispose();
                texturePtr = null;
            }
        }



        public void AttachRenderTarget()
        {
            Lock();
            try
            {
                IntPtr surface;

                renderTarget.getCustomAttribute("DDBACKBUFFER", out surface); //DX9

                //renderTarget.getCustomAttribute("SHAREDHANDLE", out surface); //DX11

                SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface, true);

                isImageSourceValid = true;
            }
            catch (Exception ex)
            {
                Debug.Print("Erro AttachRenderTarget: " + ex);
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
                isImageSourceValid = false;
            }
        }

        protected virtual void DetachRenderTarget(bool detatchSurface, bool detatchEvent)
        {
            if (detatchSurface && isImageSourceValid)
            {
                Lock();
                SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                Unlock();

                isImageSourceValid = false;
            }
        }

        public void RenderOneFrame()
        {
            if (root != null)
            {
                root.renderOneFrame();
            }

            Lock();
            AddDirtyRect(new Int32Rect(0, 0, (int)ViewportSize.Width, (int)ViewportSize.Height));
            Unlock();
        }

        public void StopRendering()
        {
            renderTimer?.Stop();
            renderTimer = null;
        }

        public void Dispose()
        {
            root.queueEndRendering();
            CompositorManager.getSingleton().removeAll();
            GC.Collect();
        }

        #region export image

        public static string CriaResourceGroup(string name)
        {
            //Cria
            if (!ResourceGroupManager.getSingleton().resourceGroupExists(name))
                ResourceGroupManager.getSingleton().createResourceGroup(name);

            //Inicializa
            if (!ResourceGroupManager.getSingleton().isResourceGroupInitialised(name))
                ResourceGroupManager.getSingleton().initialiseResourceGroup(name);

            //Carrega
            if (!ResourceGroupManager.getSingleton().isResourceGroupLoaded(name))
                ResourceGroupManager.getSingleton().loadResourceGroup(name);

            return name;
        }

        public unsafe void SalvaImagem(int vWidth = 1920, int vHeight = 1080)
        {
            string vArquivo = @"C:\Users\Admin\Pictures\output.png";

            //Tive que criar um resource group diferente para controlar a alocação de memória
            if (!ResourceGroupManager.getSingleton().resourceGroupExists("ControleRAMImg"))
                CriaResourceGroup("ControleRAMImg");

            uint w;
            uint h;

            if (vWidth           == 0 || vHeight == 0)
            {
                w = (uint)this.Width;
                h = (uint)this.Height;
            }
            else
            {
                w = (uint)vWidth;
                h = (uint)vHeight;
            }

            TexturePtr renderTexture;
            string vRenderName = "renderTex";

            renderTexture = TextureManager.getSingleton().createManual(vRenderName,
                            "ControleRAMImg",
                            TextureType.TEX_TYPE_2D, w, h, 32, 0,
                            org.ogre.PixelFormat.PF_A8R8G8B8,
                            (int)TextureUsage.TU_RENDERTARGET, null, false, 8, "[Quality]");

            //Esse método funciona sem alocação de memória
            //Talvez seja mais rápido
            using (HardwarePixelBufferPtr buffer = renderTexture.getBuffer())
            {
                using (RenderTexture renderTextureX = buffer.getRenderTarget())
                {
                    //Create pixelbox
                    byte[] data = new byte[PixelUtil.getMemorySize((uint)w, (uint)h, 1, renderTexture.getFormat())];
                    //Informa o computador o quanto de memoria será alocada
                    GCHandle gch = GCHandle.Alloc(data, GCHandleType.Pinned);
                    try
                    {
                        //Pega o endereço de memoria
                        IntPtr dataPtr = gch.AddrOfPinnedObject();
                        PixelBox finalPicturePixelBox = new PixelBox(w, h, 1, renderTexture.getFormat(), dataPtr);

                        // Remove todas as viewports anteriores
                        renderTextureX.removeAllViewports();
                        //Adiciona uma viewport com a camera principal
                        renderTextureX.addViewport(cam);

                        // Desativa os overlays
                        Viewport viewport = renderTextureX.getViewport(0);
                        viewport.setBackgroundColour(new ColourValue(0f, 0f, 0f, 0f));
                        viewport.setClearEveryFrame(true);
                        viewport.setOverlaysEnabled(false);
                        //cam.getViewport().setOverlaysEnabled(false);

                        //Renderiza
                        renderTextureX.update();

                        //Escreve o arquivo no disco
                        renderTextureX.writeContentsToFile(vArquivo);
                    }
                    finally
                    {
                        //Desaloca a memoria
                        gch.Free();
                    }
                }

                //Para liberar a textura
                TextureManager.getSingleton().remove(renderTexture.getHandle());
                renderTexture.Dispose();


            }
        }

        #endregion
    }
}