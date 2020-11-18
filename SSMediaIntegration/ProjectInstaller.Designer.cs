namespace SSMediaIntegration
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SSMIProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.SSMIInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // SSMIProcessInstaller
            // 
            this.SSMIProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.SSMIProcessInstaller.Password = null;
            this.SSMIProcessInstaller.Username = null;
            // 
            // SSMIInstaller
            // 
            this.SSMIInstaller.Description = "Integrates with various music providers and displays track information on a Steel" +
    "Series keyboard";
            this.SSMIInstaller.DisplayName = "SSMI";
            this.SSMIInstaller.ServiceName = "SSMIService";
            this.SSMIInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.SSMIInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.serviceInstaller1_AfterInstall);
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.SSMIProcessInstaller,
            this.SSMIInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller SSMIProcessInstaller;
        private System.ServiceProcess.ServiceInstaller SSMIInstaller;
    }
}