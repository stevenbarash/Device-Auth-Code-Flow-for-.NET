using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Timers;
using System.Collections.Generic;

namespace WindowsFormsApp1
{
    // The main form class.
    public partial class Form1 : Form
    {
        // Declare a timer for polling and a string to store the device code.
        private System.Timers.Timer aTimer;
        private string deviceCode;

        // The constructor of the Form.
        public Form1()
        {
            InitializeComponent();
        }

        // The event handler for the click event of the login button.
        private async void loginButton_Click(object sender, EventArgs e)
        {
            // Call the DeviceAuthorizationAsync method.
            await DeviceAuthorizationAsync();
        }

        // The method that requests device authorization from Okta.
        private async Task DeviceAuthorizationAsync()
        {
            // Specify the use of TLS 1.2
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // Define client ID and the device authorization endpoint.
            string clientId = "0oa6mp1jjjf4deYil1d7";
            string deviceAuthorizationEndpoint = "https://steven.oktapreview.com/oauth2/default/v1/device/authorize";

            // Initialize a HttpClient object.
            HttpClient client = new HttpClient();

            // Prepare the request.
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, deviceAuthorizationEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"client_id", clientId},
                {"scope", "openid profile offline_access"}
            });

            // Send the request.
            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                // Parse the response.
                string responseString = await response.Content.ReadAsStringAsync();
                dynamic responseObject = JsonConvert.DeserializeObject(responseString);

                // Extract the device code, user code, and verification URI from the response.
                deviceCode = responseObject.device_code;
                string userCode = responseObject.user_code;
                string verificationUri = responseObject.verification_uri;

                // Open the verification URI in the user's browser and display the user code.
                System.Diagnostics.Process.Start(verificationUri);
                MessageBox.Show(this, $"Verify yourself {userCode}.", "Authorization Pending", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Start polling the token endpoint.
                StartPollingTokenEndpoint();
            }
            else
            {
                // Handle errors.
                MessageBox.Show(this, $"Error: {response.StatusCode}", "Authorization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Method to display message boxes.
        private void SetMessageBox(string text, string title, MessageBoxIcon icon)
        {
            if (this.InvokeRequired)
            {
                // Invoke UI thread if necessary.
                this.Invoke(new Action(() => MessageBox.Show(this, text, title, MessageBoxButtons.OK, icon)));
            }
            else
            {
                MessageBox.Show(this, text, title, MessageBoxButtons.OK, icon);
            }
        }

        // Method to start a timer for polling the token endpoint.
        private void StartPollingTokenEndpoint()
        {
            aTimer = new System.Timers.Timer(5000); // poll every 5 seconds
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        // The event handler for the Elapsed event of the timer.
        private async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            string clientId = "0oa6mp1jjjf4deYil1d7";
            string tokenEndpoint = "https://steven.oktapreview.com/oauth2/default/v1/token";

            HttpClient client = new HttpClient();

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
            request.Content = new StringContent($"client_id={clientId}&device_code={deviceCode}&grant_type=urn:ietf:params:oauth:grant-type:device_code", Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                // Parse the response and extract the access token.
                string responseString = await response.Content.ReadAsStringAsync();
                dynamic responseObject = JsonConvert.DeserializeObject(responseString);

                aTimer.Stop();
                aTimer.Dispose();

                string accessToken = responseObject.access_token;
                SetMessageBox($"Access token: {accessToken}", "Authorization Successful", MessageBoxIcon.Information);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                // The user has not yet completed the authorization process. Continue polling.
            }
            else
            {
                // Handle other errors.
                aTimer.Stop();
                aTimer.Dispose();

                SetMessageBox($"Error: {response.StatusCode}", "Authorization Error", MessageBoxIcon.Error);
            }
        }
    }
}
