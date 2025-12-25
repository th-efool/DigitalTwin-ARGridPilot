using System;
using System.IO.Ports;
using UnityEngine;

public class BorderlessFullscreen : MonoBehaviour
{
    void Awake()
    {
        var r = Screen.currentResolution;
        Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
    }
}

public class PiezoSerialReceiver : MonoBehaviour
{
    [Header("Serial Settings")]
    [SerializeField] private string portName = "COM7";   // CHANGE THIS TO YOUR ARDUINO PORT
    [SerializeField] private int baudRate = 115200;

    private SerialPort serialPort;
    private string buffer = "";

    [Header("Status (read only)")]
    public bool arduinoConnected = false;
    public string arduinoStatus = "Not Connected";
    public int lastSensorIndex = -1;

    void Start()
    {
        // Debug: show all ports Unity can see
        Debug.Log("[PiezoSerialReceiver] Available ports: " +
                  string.Join(", ", SerialPort.GetPortNames()));

        try
        {
            serialPort = new SerialPort(portName, baudRate)
            {
                ReadTimeout = 1, // very small, we won't use ReadLine, only ReadExisting
                NewLine = "\n"
            };
            serialPort.Open();

            arduinoConnected = true;
            arduinoStatus = $"Connected to {portName} at {baudRate}";
            Debug.Log("[PiezoSerialReceiver] " + arduinoStatus);
        }
        catch (Exception ex)
        {
            arduinoConnected = false;
            arduinoStatus = "Failed to connect to " + portName;
            Debug.LogError($"[PiezoSerialReceiver] {arduinoStatus}: {ex.Message}");
        }
    }

    void OnDestroy()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try { serialPort.Close(); } catch { }
        }
    }

    void Update()
    {
        // 1) Read raw serial bytes if connected
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string incoming = serialPort.ReadExisting(); // non-blocking
                if (!string.IsNullOrEmpty(incoming))
                {
                    // Debug: see exactly what Arduino is sending
                    Debug.Log("[PiezoSerialReceiver] RAW: " + EscapeForLog(incoming));

                    buffer += incoming;

                    // Process complete lines (terminated by '\n')
                    ProcessBuffer();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[PiezoSerialReceiver] Serial error: " + ex.Message);
            }
        }

        // 2) Optional: show status on screen
        // (you can delete OnGUI if you don't want UI)
    }

    private void ProcessBuffer()
    {
        while (true)
        {
            int newlineIndex = buffer.IndexOf('\n');
            if (newlineIndex < 0) break; // no full line yet

            string line = buffer.Substring(0, newlineIndex);
            buffer = buffer.Substring(newlineIndex + 1);

            line = line.Trim('\r', '\n', ' ', '\t');
            if (line.Length == 0) continue;

            Debug.Log("[PiezoSerialReceiver] LINE: '" + line + "'");

            // Case 1: pure integer like "0", "1", "2"
            if (int.TryParse(line, out int sensorIndex))
            {
                HandlePiezoHit(sensorIndex);
                continue;
            }

            // Case 2: "SENSOR,2" style
            string[] parts = line.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[1], out sensorIndex))
            {
                Debug.Log("[PiezoSerialReceiver] Parsed SENSOR index: " + sensorIndex);
                HandlePiezoHit(sensorIndex);
            }
            else
            {
                Debug.LogWarning("[PiezoSerialReceiver] Cannot parse line: '" + line + "'");
            }
        }
    }

    private void HandlePiezoHit(int sensorIndex)
    {
        lastSensorIndex = sensorIndex;
        Debug.Log("[PiezoSerialReceiver] Piezo hit index = " + sensorIndex);

        var gm = GridMaster.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[PiezoSerialReceiver] Got piezo " + sensorIndex + " but GridMaster.Instance is null");
            return;
        }

        int maxIndex = gm.Length * gm.Length;
        if (sensorIndex < 0 || sensorIndex >= maxIndex)
        {
            Debug.LogWarning($"[PiezoSerialReceiver] sensorIndex {sensorIndex} out of range 0..{maxIndex - 1}");
            return;
        }

        Debug.Log($"[PiezoSerialReceiver] Moving robot to tile {sensorIndex}");
        gm.MovePlayerTo(sensorIndex);
    }

    // Just to see invisible characters (like \r, \n) in logs if needed
    private string EscapeForLog(string s)
    {
        return s
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 25), "Arduino: " + arduinoStatus);
        GUI.Label(new Rect(10, 30, 400, 25), "Last Sensor Index: " + lastSensorIndex);
    }
}
