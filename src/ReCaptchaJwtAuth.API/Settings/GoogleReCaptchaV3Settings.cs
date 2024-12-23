namespace ReCaptchaJwtAuth.API.Settings
{
    public class GoogleReCaptchaV3Settings
    {
        /// <summary>
        /// The secret key provided by Google reCAPTCHA for server-side validation.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// The site key provided by Google reCAPTCHA for client-side integration.
        /// </summary>
        public string SiteKey { get; set; } = string.Empty;

        /// <summary>
        /// The threshold score for validating reCAPTCHA responses.
        /// Responses scoring below this value will be considered invalid.
        /// </summary>
        public decimal Threshold { get; set; }
    }
}
