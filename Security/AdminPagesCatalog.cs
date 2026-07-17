namespace Kafo.Web.Security;

public record AdminPageDefinition(string SectionName, string PageName, string PagePath);

public static class AdminPagesCatalog
{
    public static IReadOnlyList<AdminPageDefinition> Pages { get; } = new List<AdminPageDefinition>
    {
        new("الرئيسية", "لوحة المعلومات", "/Admin"),
        new("الرئيسية", "إدارة الصفحة الرئيسية", "/Admin/Sliders"),
        new("الرئيسية", "الأرقام والإحصائيات", "/Admin/HomeStatistics"),
        new("الرئيسية", "أهداف الجمعية", "/Admin/StrategicGoals"),

        new("عن الجمعية", "من نحن", "/Admin/Pages/About"),
        new("عن الجمعية", "الرؤية والرسالة", "/Admin/Pages/Vision"),
        new("عن الجمعية", "كلمة رئيس الجمعية", "/Admin/Pages/President"),
        new("عن الجمعية", "كلمة المدير التنفيذي", "/Admin/Pages/ExecutiveManager"),
        new("عن الجمعية", "فريق العمل", "/Admin/TeamMembers"),
        new("عن الجمعية", "التراخيص", "/Admin/Licenses"),
        new("عن الجمعية", "الهيكل التنظيمي", "/Admin/Pages/OrganizationalStructure"),
        new("عن الجمعية", "الحسابات البنكية", "/Admin/BankAccounts"),

        new("البرامج والخدمات", "البرامج والمشاريع", "/Admin/Programs"),
        new("البرامج والخدمات", "روابط الخدمات", "/Admin/ServiceLinks"),
        new("البرامج والخدمات", "طلبات التبرع العيني", "/Admin/InKindDonationRequests"),
        new("البرامج والخدمات", "تسجيل مستفيد جديد", "/Admin/Pages/NewBeneficiaryRegistration"),
        new("البرامج والخدمات", "تحديث بيانات المستفيد", "/Admin/Pages/BeneficiaryDataUpdate"),

        new("الداعمون والأثر", "إدارة الداعمين", "/Admin/Donors"),
        new("الداعمون والأثر", "إدارة الجهات والشركات", "/Admin/Organizations"),

        new("المركز الإعلامي", "الأخبار", "/Admin/News"),
        new("المركز الإعلامي", "مكتبة الفيديو", "/Admin/Videos"),
        new("المركز الإعلامي", "شركاء النجاح", "/Admin/SuccessPartners"),
        new("المركز الإعلامي", "تقارير المساعدات", "/Admin/AssistanceReports"),

        new("ملفات الحوكمة", "أعضاء الجمعية العمومية", "/Admin/GeneralAssemblyMembers"),
        new("ملفات الحوكمة", "محاضر الجمعية العمومية", "/Admin/GeneralAssemblyMinutes"),
        new("ملفات الحوكمة", "أعضاء مجلس الإدارة", "/Admin/BoardMembers"),
        new("ملفات الحوكمة", "محاضر مجلس الإدارة", "/Admin/BoardMinutes"),
        new("ملفات الحوكمة", "لجان الجمعية", "/Admin/Committees"),
        new("ملفات الحوكمة", "السياسات واللوائح", "/Admin/Policies"),
        new("ملفات الحوكمة", "التقارير السنوية", "/Admin/AnnualReports"),
        new("ملفات الحوكمة", "التقارير الربعية", "/Admin/QuarterReports"),
        new("ملفات الحوكمة", "القوائم المالية", "/Admin/FinancialStatements"),
        new("ملفات الحوكمة", "الخطط التشغيلية", "/Admin/OperationalPlans"),

        new("التطوع والتوظيف", "الفرص التطوعية", "/Admin/VolunteerOpportunities"),
        new("التطوع والتوظيف", "الوظائف", "/Admin/Jobs"),
        new("التطوع والتوظيف", "مبادرات الجمعية", "/Admin/Initiatives"),
        new("التطوع والتوظيف", "طلبات التوظيف", "/Admin/JobApplications"),
        new("التطوع والتوظيف", "طلبات التطوع", "/Admin/VolunteerRequests"),

        new("التواصل والإعدادات", "صندوق الرسائل", "/Admin/Messages"),
        new("التواصل والإعدادات", "قياس الرضا", "/Admin/Satisfaction"),
        new("التواصل والإعدادات", "التغذية الراجعة", "/Admin/Feedback"),
        new("التواصل والإعدادات", "الاستبيانات", "/Admin/Surveys"),
        new("التواصل والإعدادات", "الإعدادات العامة", "/Admin/Settings"),
        new("التواصل والإعدادات", "مستخدمي لوحة التحكم والصلاحيات", "/Admin/Users"),
        new("التواصل والإعدادات", "الملف الشخصي", "/Admin/Profile")
    };

    public static AdminPageDefinition? Match(string path)
    {
        var normalized = Normalize(path);

        // The notification endpoint displays donor-management data and follows the same permission.
        if (normalized.StartsWith("/Admin/NotificationsApi", StringComparison.OrdinalIgnoreCase))
            normalized = "/Admin/Donors";

        return Pages
            .OrderByDescending(x => x.PagePath.Length)
            .FirstOrDefault(x =>
                normalized.Equals(Normalize(x.PagePath), StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith(Normalize(x.PagePath) + "/", StringComparison.OrdinalIgnoreCase));
    }

    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        path = path.Trim();

        if (path.Length > 1)
            path = path.TrimEnd('/');

        return path;
    }
}
