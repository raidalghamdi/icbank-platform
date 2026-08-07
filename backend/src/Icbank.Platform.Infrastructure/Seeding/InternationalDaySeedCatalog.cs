namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>
/// Curated catalogue of observances the Authority plans communications around, seeded as
/// reference data.
/// <para>
/// This is deliberately real data, not demo scaffolding. The dashboard's "upcoming events" panel
/// and the الأيام العالمية page both read <c>international_days</c>, so an empty table renders a
/// permanently empty landing page — the state dev has been in. Every row below is a genuine
/// observance with its organiser and a citable source, so the same catalogue is safe to seed in
/// any environment and never needs deleting before real use.
/// </para>
/// <para>
/// Fabricated activations and archive entries are pointedly NOT seeded. The KPI tiles that count
/// them stay at zero until the Authority enters its own, which is the honest reading — a number
/// invented by a seeder is worse than a zero, because it looks like a measurement.
/// </para>
/// </summary>
internal static class InternationalDaySeedCatalog
{
    /// <summary>Gets the seed rows, in calendar order.</summary>
    /// <remarks>
    /// <c>AnnualDate</c> uses the Arabic "d MMMM" form that <c>ArabicAnnualDateParser</c> reads,
    /// matching how the Authority writes dates elsewhere in the product rather than introducing a
    /// second convention. Days whose date moves year to year (Ramadan, Eid, the Hijri new year)
    /// are excluded on purpose: the column holds one fixed annual date and cannot express them.
    /// </remarks>
    public static IReadOnlyList<InternationalDaySeedRow> Rows { get; } = new List<InternationalDaySeedRow>
    {
        new(
            "اليوم العالمي للغة العربية",
            "World Arabic Language Day",
            "18 ديسمبر",
            "ثقافة",
            "اليونسكو",
            "https://www.unesco.org/en/days/arabic-language",
            "أقرّته اليونسكو عام 2010 في ذكرى قرار الجمعية العامة للأمم المتحدة عام 1973 بإدراج العربية ضمن اللغات الرسمية."),
        new(
            "اليوم العالمي للعدالة الاجتماعية",
            "World Day of Social Justice",
            "20 فبراير",
            "مجتمع",
            "الأمم المتحدة",
            "https://www.un.org/en/observances/social-justice-day",
            "أعلنته الجمعية العامة عام 2007 ويُحتفى به منذ عام 2009."),
        new(
            "اليوم الدولي للغة الأم",
            "International Mother Language Day",
            "21 فبراير",
            "ثقافة",
            "اليونسكو",
            "https://www.unesco.org/en/days/mother-language",
            "أقرّته اليونسكو عام 1999 لتعزيز التنوع اللغوي والثقافي."),
        new(
            "يوم التأسيس السعودي",
            "Saudi Founding Day",
            "22 فبراير",
            "وطني",
            "المملكة العربية السعودية",
            "https://www.mofa.gov.sa/ar/ksa/Pages/foundingday.aspx",
            "أمر ملكي صدر في 27 يناير 2022 بجعل 22 فبراير يوماً للتأسيس، إحياءً لتأسيس الدولة السعودية الأولى عام 1727م على يد الإمام محمد بن سعود."),
        new(
            "اليوم العالمي للمرأة",
            "International Women's Day",
            "8 مارس",
            "مجتمع",
            "الأمم المتحدة",
            "https://www.un.org/en/observances/womens-day",
            "تحتفي به الأمم المتحدة منذ عام 1975."),
        new(
            "اليوم العالمي لحقوق المستهلك",
            "World Consumer Rights Day",
            "15 مارس",
            "حماية المستهلك",
            "منظمة المستهلك الدولية",
            "https://www.consumersinternational.org/what-we-do/world-consumer-rights-day/",
            "يُحتفى به منذ 1983 في ذكرى خطاب الرئيس الأمريكي جون كينيدي أمام الكونغرس في 15 مارس 1962، الذي حدّد فيه أربعة حقوق أساسية للمستهلك. وثيق الصلة بعمل الهيئة."),
        new(
            "يوم الصحة العالمي",
            "World Health Day",
            "7 أبريل",
            "صحة",
            "منظمة الصحة العالمية",
            "https://www.who.int/campaigns/world-health-day",
            "يوافق ذكرى تأسيس منظمة الصحة العالمية عام 1948."),
        new(
            "اليوم العالمي للأرض",
            "International Mother Earth Day",
            "22 أبريل",
            "بيئة",
            "الأمم المتحدة",
            "https://www.un.org/en/observances/earth-day",
            "أقرّته الجمعية العامة عام 2009."),
        new(
            "اليوم العالمي لحرية الصحافة",
            "World Press Freedom Day",
            "3 مايو",
            "إعلام",
            "اليونسكو",
            "https://www.unesco.org/en/days/press-freedom",
            "أعلنته الجمعية العامة عام 1993 بناءً على توصية مؤتمر اليونسكو العام."),
        new(
            "اليوم العالمي للاتصالات ومجتمع المعلومات",
            "World Telecommunication and Information Society Day",
            "17 مايو",
            "تقنية",
            "الاتحاد الدولي للاتصالات",
            "https://www.itu.int/en/wtisd/Pages/default.aspx",
            "يوافق ذكرى تأسيس الاتحاد الدولي للاتصالات عام 1865."),
        new(
            "اليوم العالمي للبيئة",
            "World Environment Day",
            "5 يونيو",
            "بيئة",
            "برنامج الأمم المتحدة للبيئة",
            "https://www.unep.org/events/un-day/world-environment-day",
            "أقرّته الجمعية العامة عام 1972 ويُحتفى به منذ 1973."),
        new(
            "اليوم الدولي للشباب",
            "International Youth Day",
            "12 أغسطس",
            "مجتمع",
            "الأمم المتحدة",
            "https://www.un.org/en/observances/youth-day",
            "أقرّته الجمعية العامة عام 1999."),
        new(
            "اليوم الدولي للسلام",
            "International Day of Peace",
            "21 سبتمبر",
            "مجتمع",
            "الأمم المتحدة",
            "https://www.un.org/en/observances/international-day-peace",
            "أُنشئ عام 1981 وثُبّت في 21 سبتمبر عام 2001."),
        new(
            "اليوم الوطني السعودي",
            "Saudi National Day",
            "23 سبتمبر",
            "وطني",
            "المملكة العربية السعودية",
            "https://www.my.gov.sa/wps/portal/snp/aboutksa/nationalDay",
            "يوافق ذكرى توحيد المملكة وإعلان اسمها بالمرسوم الملكي الصادر عام 1932م."),
        new(
            "اليوم العالمي للمعلمين",
            "World Teachers' Day",
            "5 أكتوبر",
            "تعليم",
            "اليونسكو",
            "https://www.unesco.org/en/days/teachers",
            "يُحتفى به منذ 1994 في ذكرى توصية 1966 بشأن أوضاع المعلمين."),
        new(
            "اليوم العالمي للصحة النفسية",
            "World Mental Health Day",
            "10 أكتوبر",
            "صحة",
            "منظمة الصحة العالمية",
            "https://www.who.int/campaigns/world-mental-health-day",
            "أُطلق عام 1992 بمبادرة من الاتحاد العالمي للصحة النفسية."),
        new(
            "يوم الأمم المتحدة",
            "United Nations Day",
            "24 أكتوبر",
            "دولي",
            "الأمم المتحدة",
            "https://www.un.org/en/observances/un-day",
            "يوافق دخول ميثاق الأمم المتحدة حيّز النفاذ عام 1945."),
        new(
            "اليوم العالمي للتسامح",
            "International Day for Tolerance",
            "16 نوفمبر",
            "مجتمع",
            "اليونسكو",
            "https://www.unesco.org/en/days/tolerance",
            "أعلنته الجمعية العامة عام 1996."),
        new(
            "اليوم العالمي للطفل",
            "World Children's Day",
            "20 نوفمبر",
            "مجتمع",
            "اليونيسف",
            "https://www.unicef.org/world-childrens-day",
            "يوافق اعتماد إعلان حقوق الطفل عام 1959 واتفاقية حقوق الطفل عام 1989."),
        new(
            "اليوم الدولي للأشخاص ذوي الإعاقة",
            "International Day of Persons with Disabilities",
            "3 ديسمبر",
            "مجتمع",
            "الأمم المتحدة",
            "https://www.un.org/en/observances/day-of-persons-with-disabilities",
            "أعلنته الجمعية العامة عام 1992."),
        new(
            "اليوم العالمي للمنافسة",
            "World Competition Day",
            "5 ديسمبر",
            "منافسة",
            "الشبكة الدولية للمجتمع المدني المعنية بالمنافسة",
            "https://digitallibrary.un.org/record/432136",
            "يوافق اعتماد الجمعية العامة للأمم المتحدة القرار 35/63 في 5 ديسمبر 1980، الذي أقرّت به مجموعة المبادئ والقواعد المتفق عليها لمكافحة الممارسات التجارية التقييدية. اقترحت الشبكة الدولية للمجتمع المدني المعنية بالمنافسة اعتماده يوماً عالمياً للمنافسة في الذكرى الثلاثين. أوثق الأيام صلةً باختصاص الهيئة."),
        new(
            "اليوم الدولي لمكافحة الفساد",
            "International Anti-Corruption Day",
            "9 ديسمبر",
            "حوكمة",
            "مكتب الأمم المتحدة المعني بالمخدرات والجريمة",
            "https://www.un.org/en/observances/anti-corruption-day",
            "يوافق اعتماد اتفاقية الأمم المتحدة لمكافحة الفساد عام 2003."),
        new(
            "يوم حقوق الإنسان",
            "Human Rights Day",
            "10 ديسمبر",
            "مجتمع",
            "الأمم المتحدة",
            "https://www.un.org/en/observances/human-rights-day",
            "يوافق اعتماد الإعلان العالمي لحقوق الإنسان عام 1948."),
    };
}
