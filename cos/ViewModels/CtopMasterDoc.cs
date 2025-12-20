using System;

namespace cos.ViewModels
{
    public class CtopMasterDoc
    {
        public long id { get; set; }
        public string username { get; set; } = string.Empty;
        public string document_path { get; set; } = string.Empty;
        public string file_name { get; set; } = string.Empty;
        public string file_category { get; set; } = string.Empty;
        public string file_category_code { get; set; } = string.Empty;
        public string record_status { get; set; } = "ACTIVE";
        public long? created_by { get; set; }
        public long? updated_by { get; set; }
        public DateTime? created_on { get; set; }
        public DateTime? updated_on { get; set; }
        public string? alt_document_path { get; set; }
        public string? alt_file_name { get; set; }
    }

    public class CtopMasterDocVM
    {
        public long id { get; set; }
        public string username { get; set; } = string.Empty;
        public string file_name { get; set; } = string.Empty;
        public string file_category { get; set; } = string.Empty;
        public string file_category_code { get; set; } = string.Empty;
        public string document_path { get; set; } = string.Empty;
        public DateTime? created_on { get; set; }
        public bool hasDocument { get; set; }
        public string? alt_document_path { get; set; }
        public string? alt_file_name { get; set; }
    }

    public class DocumentUploadVM
    {
        public string username { get; set; } = string.Empty;
        public string file_category_code { get; set; } = string.Empty;
    }

    public static class DocumentCategory
    {
        public const string BA_APPROVAL_LETTER = "BA_APPROVAL";
        public const string ID_CARD = "ID_CARD";
        public const string AADHAR_CARD = "AADHAR";
        public const string PAN_CARD = "PAN_CARD";
        public const string PHOTO = "PHOTO";

        public static string GetCategoryName(string code)
        {
            return code switch
            {
                BA_APPROVAL_LETTER => "BA Approval Letter",
                ID_CARD => "Employee ID Card",
                AADHAR_CARD => "Aadhar Card",
                PAN_CARD => "PAN Card",
                PHOTO => "Photo",
                _ => code
            };
        }

        public static string GetCategoryShortCode(string code)
        {
            return code switch
            {
                BA_APPROVAL_LETTER => "BA",
                ID_CARD => "ID",
                AADHAR_CARD => "AAD",
                PAN_CARD => "PAN",
                PHOTO => "PH",
                _ => code.Substring(0, Math.Min(3, code.Length)).ToUpper()
            };
        }
    }
}

