using DreamRaytracingRP.Rendering.Layers;
using Unity.Mathematics;
using UnityEngine;

namespace DreamRaytracingRP.Rendering
{
    public class DebugView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMPro.TMP_Text worldPosField;

        public static DebugView Instance { get; private set; }

        uint frames;
        float lastTime;
        uint fps;

        void Awake()
        {
            if (Instance != null) Destroy(gameObject);
            Instance = this;
            Hide();
        }

        public void Show()
        {
            group.alpha = 1;
        }

        public void Hide()
        {
            group.alpha = 0;
        }

        public bool Showing => group.alpha > 0;

        public void ToggleView()
        {
            if (Showing) Hide();
            else Show();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
            {
                ToggleView();
            }

            if (!Showing) return;

            frames++;
            if (Time.time - lastTime > 1)
            {
                fps = frames;
                lastTime = Time.time;
                frames = 0;
            }

            GraphicsLayer.Instance.GetCameraFloatingOrigin(out var camPos, out var camOrientation);

            string text = "";

            text += $"Build: {Application.version}\n";
            text += $"Build (Unity): {Application.unityVersion}\n";

            text += $"\nWorld Position: ({RoundD(camPos.x)}, {RoundD(camPos.y)}, {RoundD(camPos.z)}) units\n";

            var orient = math.Euler(camOrientation) * math.TODEGREES;
            text += $"\nWorld Orientation: ({RoundD(orient.x)}, {RoundD(orient.y)}, {RoundD(orient.z)}) degrees\n";

            text += $"\nFPS: {fps}";

            text += $"\nSystem: {SystemInfo.operatingSystem}\n" +
                    $"System memory: {SystemInfo.systemMemorySize} MB\n"+
                    $"Processor: {SystemInfo.processorModel}\tCount: {SystemInfo.processorCount}\tFreq: {SystemInfo.processorFrequency} MHz\n" +
                    $"\nGraphics: {SystemInfo.graphicsDeviceName}, \t{SystemInfo.graphicsMemorySize} MB\n" + 
                    $"Window Size: {Screen.width} x {Screen.height}\tDPI: {Screen.dpi}\n";


            worldPosField.SetText(text);
        }

        double RoundD(double d, int decimals = 2)
        {
            return System.Math.Round(d, decimals);
        }
    }
}
