﻿namespace JQL
{
    public class ClientUI
    {
        public string FileName { set; get; } = "";
        public string TemplateName { set; get; } = "";
        public string LoadAPI { set; get; } = "";
        public string SubmitAPI { set; get; } = "";

        public bool PreventReBuilding {  set; get; } = false;

    }
}
