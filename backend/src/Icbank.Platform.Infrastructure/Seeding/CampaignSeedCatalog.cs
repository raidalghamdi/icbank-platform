using Icbank.Platform.Domain.Campaigns;

namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>
/// The department's campaign book as the authority currently runs it: five internal campaigns and
/// six external ones, spread across all four lifecycle states so both pages and every filter chip
/// have something real to show. This catalogue is the source of truth the seeder reconciles the
/// <c>campaigns</c> table against — a code missing from here is a campaign the department no longer
/// tracks, so it is removed rather than left behind on the page.
/// </summary>
internal static class CampaignSeedCatalog
{
    /// <summary>Gets the seeded campaigns, keyed by <see cref="CampaignSeedRow.Code"/>.</summary>
    internal static IReadOnlyList<CampaignSeedRow> Rows { get; } = new[]
    {
        new CampaignSeedRow(
            "INT-01",
            "حملة التعريف بنظام المنافسة لموظفي الهيئة",
            "حملة داخلية تُعرّف الموظفين بأحكام نظام المنافسة ولائحته التنفيذية وأثرها على عملهم اليومي.",
            "رفع مستوى الإلمام بنظام المنافسة لدى موظفي الهيئة إلى 90% قبل نهاية الربع.",
            CampaignAudience.Internal,
            CampaignStatus.Running,
            "ريان الغامدي",
            "الإدارة التنفيذية للتواصل المؤسسي",
            65,
            -38,
            22,
            "اكتملت الورشتان الأولى والثانية، والعمل جارٍ على إنتاج المقاطع التعريفية القصيرة.",
            980,
            14200,
            1860,
            17,
            1,
            new[]
            {
                new CampaignDeliverableSeedRow("الهوية البصرية للحملة ورسائلها الرئيسية", -34, true),
                new CampaignDeliverableSeedRow("ورشتان تعريفيتان لموظفي الإدارات التشغيلية", -12, true),
                new CampaignDeliverableSeedRow("سلسلة مقاطع تعريفية قصيرة (6 مقاطع)", 9, false),
                new CampaignDeliverableSeedRow("اختبار قياس الإلمام بعد الحملة", 20, false),
            },
            new[]
            {
                new CampaignChannelSeedRow("البريد الداخلي", 7, 620, 410),
                new CampaignChannelSeedRow("الشاشات الداخلية", 6, 540, 180),
                new CampaignChannelSeedRow("ورش العمل", 2, 180, 165),
                new CampaignChannelSeedRow("مجلة شرفة", 2, 430, 95),
            }),
        new CampaignSeedRow(
            "INT-02",
            "حملة قيم الهيئة الأربع",
            "حملة تُترجم قيم الهيئة إلى سلوكيات عمل ملموسة عبر قصص من داخل الإدارات.",
            "ربط كل قيمة من قيم الهيئة بسلوك عملي واحد على الأقل معروف لدى الموظفين.",
            CampaignAudience.Internal,
            CampaignStatus.Running,
            "لمى العتيبي",
            "إدارة الاتصال الداخلي",
            42,
            -21,
            48,
            "نُشرت قيمتان من أصل أربع، والعمل جارٍ على تصوير قصص القيمة الثالثة.",
            760,
            9100,
            1240,
            11,
            2,
            new[]
            {
                new CampaignDeliverableSeedRow("دليل القيم وسلوكياتها المعتمد", -18, true),
                new CampaignDeliverableSeedRow("قصص القيمة الأولى والثانية", -4, true),
                new CampaignDeliverableSeedRow("قصص القيمة الثالثة والرابعة", 26, false),
                new CampaignDeliverableSeedRow("لقاء ختامي لتكريم السلوكيات المتميزة", 45, false),
            },
            new[]
            {
                new CampaignChannelSeedRow("البريد الداخلي", 4, 480, 320),
                new CampaignChannelSeedRow("الشاشات الداخلية", 5, 520, 210),
                new CampaignChannelSeedRow("تطبيق الهيئة الداخلي", 2, 300, 190),
            }),
        new CampaignSeedRow(
            "INT-03",
            "حملة التحول الرقمي الداخلي 2026",
            "حملة مواكبة لإطلاق الأنظمة الرقمية الجديدة وتهيئة الموظفين لاستخدامها.",
            "تهيئة 100% من الموظفين لاستخدام الأنظمة الرقمية الجديدة قبل الإطلاق الرسمي.",
            CampaignAudience.Internal,
            CampaignStatus.UnderReview,
            "عبدالله القحطاني",
            "إدارة الاتصال الداخلي",
            88,
            -74,
            9,
            "المواد كاملة ومرفوعة، وبانتظار اعتماد الإدارة التنفيذية للرسالة الختامية.",
            1120,
            16800,
            2310,
            23,
            3,
            new[]
            {
                new CampaignDeliverableSeedRow("خطة الحملة ورسائلها المعتمدة", -70, true),
                new CampaignDeliverableSeedRow("أدلة الاستخدام المصورة للأنظمة", -30, true),
                new CampaignDeliverableSeedRow("جلسات تدريب مباشرة لكل إدارة", -8, true),
                new CampaignDeliverableSeedRow("الرسالة الختامية وتقرير النتائج", 7, false),
            },
            new[]
            {
                new CampaignChannelSeedRow("البريد الداخلي", 9, 840, 610),
                new CampaignChannelSeedRow("تطبيق الهيئة الداخلي", 8, 720, 940),
                new CampaignChannelSeedRow("ورش العمل", 6, 410, 380),
            }),
        new CampaignSeedRow(
            "INT-04",
            "حملة سلامة بيئة العمل",
            "حملة توعوية بإجراءات السلامة والإخلاء في مقر الهيئة وفروعها.",
            "وصول تعليمات الإخلاء والسلامة إلى كل موظف قبل تمرين الإخلاء السنوي.",
            CampaignAudience.Internal,
            CampaignStatus.Upcoming,
            "نورة الشهري",
            "إدارة الاتصال الداخلي",
            12,
            14,
            72,
            "اعتُمدت الفكرة والميزانية، والعمل جارٍ على تجهيز المواد قبل الانطلاق.",
            0,
            0,
            0,
            0,
            4,
            new[]
            {
                new CampaignDeliverableSeedRow("خطة الحملة وجدولها الزمني", 10, true),
                new CampaignDeliverableSeedRow("ملصقات مسارات الإخلاء", 25, false),
                new CampaignDeliverableSeedRow("مقطع تعريفي بإجراءات السلامة", 40, false),
                new CampaignDeliverableSeedRow("تمرين الإخلاء السنوي", 68, false),
            },
            new[]
            {
                new CampaignChannelSeedRow("البريد الداخلي", 0, 0, 0),
                new CampaignChannelSeedRow("الشاشات الداخلية", 0, 0, 0),
            }),
        new CampaignSeedRow(
            "INT-05",
            "حملة اليوم الوطني — الاحتفال الداخلي",
            "حملة داخلية للاحتفاء باليوم الوطني داخل مقر الهيئة وإشراك الموظفين في محتواها.",
            "إشراك 70% من الموظفين في أنشطة اليوم الوطني الداخلية.",
            CampaignAudience.Internal,
            CampaignStatus.Completed,
            "لمى العتيبي",
            "الإدارة التنفيذية للتواصل المؤسسي",
            100,
            -128,
            -96,
            "اكتملت الحملة واعتُمد تقريرها الختامي، ومشاركة الموظفين بلغت 78%.",
            1240,
            18400,
            3120,
            21,
            5,
            new[]
            {
                new CampaignDeliverableSeedRow("تصاميم اليوم الوطني بهوية الهيئة", -120, true),
                new CampaignDeliverableSeedRow("الفعالية الداخلية في المقر", -100, true),
                new CampaignDeliverableSeedRow("مسابقة الموظفين", -99, true),
                new CampaignDeliverableSeedRow("التقرير الختامي للحملة", -96, true),
            },
            new[]
            {
                new CampaignChannelSeedRow("الشاشات الداخلية", 8, 620, 240),
                new CampaignChannelSeedRow("البريد الداخلي", 6, 610, 520),
                new CampaignChannelSeedRow("تطبيق الهيئة الداخلي", 7, 480, 2360),
            }),
        new CampaignSeedRow(
            "EXT-01",
            "حملة الإبلاغ عن الممارسات الاحتكارية",
            "حملة تُعرّف الجمهور بقنوات الإبلاغ عن الممارسات المخلة بالمنافسة وتشجع على استخدامها.",
            "زيادة البلاغات المكتملة البيانات الواردة عبر القنوات الرسمية بنسبة 30%.",
            CampaignAudience.External,
            CampaignStatus.Running,
            "ريان الغامدي",
            "الإدارة التنفيذية للتواصل المؤسسي",
            72,
            -46,
            18,
            "المرحلة الثانية منشورة على القنوات، ومعدل التفاعل أعلى من المستهدف بنحو 12%.",
            412000,
            1860000,
            38400,
            34,
            1,
            new[]
            {
                new CampaignDeliverableSeedRow("الهوية البصرية والرسائل الرئيسية", -42, true),
                new CampaignDeliverableSeedRow("مقطع الحملة الرئيسي", -28, true),
                new CampaignDeliverableSeedRow("سلسلة إنفوجرافيك قنوات الإبلاغ", -6, true),
                new CampaignDeliverableSeedRow("تقرير الأثر ونتائج الحملة", 16, false),
            },
            new[]
            {
                new CampaignChannelSeedRow("منصة إكس", 14, 268000, 21400),
                new CampaignChannelSeedRow("لينكدإن", 8, 96000, 9800),
                new CampaignChannelSeedRow("إنستقرام", 7, 74000, 6200),
                new CampaignChannelSeedRow("الموقع الإلكتروني", 5, 31000, 1000),
            }),
        new CampaignSeedRow(
            "EXT-02",
            "حملة توعية المنشآت بأحكام الاندماج والاستحواذ",
            "حملة موجهة لقطاع الأعمال للتعريف بإجراءات الإشعار عن الاندماج والاستحواذ ومواعيدها.",
            "رفع نسبة الإشعارات المكتملة من أول مرة لدى المنشآت المستهدفة.",
            CampaignAudience.External,
            CampaignStatus.Running,
            "عبدالله القحطاني",
            "إدارة الاتصال الخارجي",
            54,
            -30,
            40,
            "اكتمل الدليل المبسط وعُقدت ورشة القطاع الأولى، وتبقّت ورشتان.",
            186000,
            720000,
            14900,
            19,
            2,
            new[]
            {
                new CampaignDeliverableSeedRow("الدليل المبسط لإجراءات الإشعار", -22, true),
                new CampaignDeliverableSeedRow("ورشة القطاع الأولى", -9, true),
                new CampaignDeliverableSeedRow("ورشتا القطاع الثانية والثالثة", 24, false),
                new CampaignDeliverableSeedRow("حزمة أسئلة شائعة على الموقع", 36, false),
            },
            new[]
            {
                new CampaignChannelSeedRow("لينكدإن", 9, 112000, 9600),
                new CampaignChannelSeedRow("منصة إكس", 6, 58000, 4300),
                new CampaignChannelSeedRow("الصحافة والإعلام", 2, 34000, 600),
                new CampaignChannelSeedRow("الموقع الإلكتروني", 2, 21000, 400),
            }),
        new CampaignSeedRow(
            "EXT-03",
            "حملة أسعار السلع الأساسية",
            "حملة تُوضح دور الهيئة في مراقبة الممارسات السعرية وتصحيح المفاهيم المتداولة.",
            "تصحيح أبرز ثلاث مفاهيم مغلوطة عن دور الهيئة في تحديد الأسعار.",
            CampaignAudience.External,
            CampaignStatus.UnderReview,
            "نورة الشهري",
            "إدارة الاتصال الخارجي",
            90,
            -62,
            6,
            "المواد كاملة ونتائج القياس جاهزة، وبانتظار اعتماد التقرير الختامي.",
            524000,
            2140000,
            46200,
            28,
            3,
            new[]
            {
                new CampaignDeliverableSeedRow("خطة الرسائل ومصفوفة التصحيح", -58, true),
                new CampaignDeliverableSeedRow("مقاطع الخبراء التوضيحية", -34, true),
                new CampaignDeliverableSeedRow("بيان صحفي ولقاءات إعلامية", -11, true),
                new CampaignDeliverableSeedRow("تقرير القياس والأثر", 4, false),
            },
            new[]
            {
                new CampaignChannelSeedRow("منصة إكس", 12, 302000, 26800),
                new CampaignChannelSeedRow("الصحافة والإعلام", 6, 118000, 8400),
                new CampaignChannelSeedRow("إنستقرام", 6, 74000, 9200),
                new CampaignChannelSeedRow("الإذاعة", 4, 46000, 1800),
            }),
        new CampaignSeedRow(
            "EXT-04",
            "حملة اليوم العالمي للمنافسة",
            "حملة مناسباتية تُبرز أثر المنافسة العادلة على المستهلك والاقتصاد.",
            "الوصول إلى مليون ظهور خلال أسبوع المناسبة.",
            CampaignAudience.External,
            CampaignStatus.Upcoming,
            "ريان الغامدي",
            "الإدارة التنفيذية للتواصل المؤسسي",
            18,
            26,
            54,
            "اعتُمدت الفكرة الإبداعية، والعمل جارٍ على إنتاج المواد البصرية.",
            0,
            0,
            0,
            0,
            4,
            new[]
            {
                new CampaignDeliverableSeedRow("الفكرة الإبداعية المعتمدة", 12, true),
                new CampaignDeliverableSeedRow("حزمة التصاميم والمقاطع", 30, false),
                new CampaignDeliverableSeedRow("شراكة إعلامية للمناسبة", 38, false),
                new CampaignDeliverableSeedRow("تقرير أسبوع المناسبة", 52, false),
            },
            new[]
            {
                new CampaignChannelSeedRow("منصة إكس", 0, 0, 0),
                new CampaignChannelSeedRow("لينكدإن", 0, 0, 0),
                new CampaignChannelSeedRow("الصحافة والإعلام", 0, 0, 0),
            }),
        new CampaignSeedRow(
            "EXT-05",
            "حملة دليل المستهلك الذكي",
            "حملة تعريفية بحقوق المستهلك في السوق التنافسي وكيفية المقارنة بين العروض.",
            "توزيع الدليل المبسط على مليون مستهلك عبر القنوات الرقمية.",
            CampaignAudience.External,
            CampaignStatus.Upcoming,
            "لمى العتيبي",
            "إدارة الاتصال الخارجي",
            8,
            40,
            96,
            "الحملة في مرحلة إعداد المحتوى التحريري قبل مرحلة التصميم.",
            0,
            0,
            0,
            0,
            5,
            new[]
            {
                new CampaignDeliverableSeedRow("المحتوى التحريري للدليل", 34, false),
                new CampaignDeliverableSeedRow("تصميم الدليل ونسخته التفاعلية", 58, false),
                new CampaignDeliverableSeedRow("حزمة نشر على القنوات الرقمية", 78, false),
                new CampaignDeliverableSeedRow("تقرير الأثر", 94, false),
            },
            new[]
            {
                new CampaignChannelSeedRow("إنستقرام", 0, 0, 0),
                new CampaignChannelSeedRow("منصة إكس", 0, 0, 0),
                new CampaignChannelSeedRow("الموقع الإلكتروني", 0, 0, 0),
            }),
        new CampaignSeedRow(
            "EXT-06",
            "حملة المنافسة العادلة في التجارة الإلكترونية",
            "حملة تُعرّف المتاجر الإلكترونية والمستهلكين بضوابط المنافسة في السوق الرقمي.",
            "رفع وعي المتاجر الإلكترونية بالممارسات المحظورة في السوق الرقمي.",
            CampaignAudience.External,
            CampaignStatus.Completed,
            "عبدالله القحطاني",
            "إدارة الاتصال الخارجي",
            100,
            -156,
            -104,
            "اكتملت الحملة واعتُمد تقريرها الختامي، والظهور تجاوز المستهدف بنسبة 22%.",
            688000,
            2960000,
            61800,
            41,
            6,
            new[]
            {
                new CampaignDeliverableSeedRow("مصفوفة الرسائل والضوابط", -150, true),
                new CampaignDeliverableSeedRow("سلسلة مقاطع الضوابط الرقمية", -132, true),
                new CampaignDeliverableSeedRow("لقاء مع منصات التجارة الإلكترونية", -118, true),
                new CampaignDeliverableSeedRow("التقرير الختامي للحملة", -104, true),
            },
            new[]
            {
                new CampaignChannelSeedRow("منصة إكس", 18, 386000, 33400),
                new CampaignChannelSeedRow("لينكدإن", 11, 164000, 18600),
                new CampaignChannelSeedRow("الصحافة والإعلام", 7, 92000, 7100),
                new CampaignChannelSeedRow("الموقع الإلكتروني", 5, 46000, 2700),
            }),
    };
}
