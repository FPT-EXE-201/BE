using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence.Seeders;

public static class MotivationalTemplateSeeder
{
    private static readonly DateTime SeedDate = new(2026, 2, 21, 0, 0, 0, DateTimeKind.Utc);

    public static void Seed(ModelBuilder builder)
    {
        // ═══════════════════════════════════════════════════
        // BABY_SIZE — So sánh kích thước bé theo tuần thai
        // ═══════════════════════════════════════════════════

        var babySizeTemplates = new (string id, int weekStart, int weekEnd, string variablesJson)[]
        {
            ("c6000001-0000-0000-0000-000000000001", 4, 5,   """{"fruitVi":"hạt mè","fruitEn":"poppy seed","sizeCm":"0.1"}"""),
            ("c6000001-0000-0000-0000-000000000002", 6, 7,   """{"fruitVi":"hạt đậu lăng","fruitEn":"lentil","sizeCm":"0.6"}"""),
            ("c6000001-0000-0000-0000-000000000003", 8, 9,   """{"fruitVi":"quả mâm xôi","fruitEn":"raspberry","sizeCm":"1.6"}"""),
            ("c6000001-0000-0000-0000-000000000004", 10, 11, """{"fruitVi":"quả mận","fruitEn":"prune","sizeCm":"3.1"}"""),
            ("c6000001-0000-0000-0000-000000000005", 12, 13, """{"fruitVi":"quả chanh","fruitEn":"lime","sizeCm":"5.4"}"""),
            ("c6000001-0000-0000-0000-000000000006", 14, 15, """{"fruitVi":"quả cam","fruitEn":"orange","sizeCm":"8.7"}"""),
            ("c6000001-0000-0000-0000-000000000007", 16, 17, """{"fruitVi":"quả bơ","fruitEn":"avocado","sizeCm":"11.6"}"""),
            ("c6000001-0000-0000-0000-000000000008", 18, 19, """{"fruitVi":"quả xoài","fruitEn":"mango","sizeCm":"15.3"}"""),
            ("c6000001-0000-0000-0000-000000000009", 20, 21, """{"fruitVi":"quả chuối","fruitEn":"banana","sizeCm":"25.6"}"""),
            ("c6000001-0000-0000-0000-00000000000a", 22, 23, """{"fruitVi":"quả bắp","fruitEn":"corn","sizeCm":"28.9"}"""),
            ("c6000001-0000-0000-0000-00000000000b", 24, 25, """{"fruitVi":"quả dưa lưới","fruitEn":"cantaloupe","sizeCm":"30.0"}"""),
            ("c6000001-0000-0000-0000-00000000000c", 26, 27, """{"fruitVi":"bông cải xanh","fruitEn":"broccoli","sizeCm":"36.6"}"""),
            ("c6000001-0000-0000-0000-00000000000d", 28, 29, """{"fruitVi":"quả bí ngô","fruitEn":"butternut squash","sizeCm":"38.6"}"""),
            ("c6000001-0000-0000-0000-00000000000e", 30, 31, """{"fruitVi":"quả dừa","fruitEn":"coconut","sizeCm":"40.0"}"""),
            ("c6000001-0000-0000-0000-00000000000f", 32, 33, """{"fruitVi":"quả dứa","fruitEn":"pineapple","sizeCm":"42.4"}"""),
            ("c6000001-0000-0000-0000-000000000010", 34, 35, """{"fruitVi":"quả dưa hấu","fruitEn":"honeydew melon","sizeCm":"45.0"}"""),
            ("c6000001-0000-0000-0000-000000000011", 36, 37, """{"fruitVi":"quả bưởi","fruitEn":"papaya","sizeCm":"47.4"}"""),
            ("c6000001-0000-0000-0000-000000000012", 38, 40, """{"fruitVi":"quả dưa hấu","fruitEn":"watermelon","sizeCm":"50.0"}"""),
        };

        foreach (var (id, weekStart, weekEnd, variablesJson) in babySizeTemplates)
        {
            builder.Entity<MotivationalTemplate>().HasData(new
            {
                Id = new Guid(id),
                Category = MotivationalCategory.BabySize,
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                IsActive = true,
                VariablesJson = variablesJson,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate
            });
        }

        // ═══════════════════════════════════════════════════
        // MILESTONE — Cột mốc phát triển
        // ═══════════════════════════════════════════════════

        var milestoneTemplates = new (string id, int weekStart, int weekEnd)[]
        {
            ("c6000002-0000-0000-0000-000000000001", 8, 9),
            ("c6000002-0000-0000-0000-000000000002", 12, 13),
            ("c6000002-0000-0000-0000-000000000003", 16, 17),
            ("c6000002-0000-0000-0000-000000000004", 20, 21),
            ("c6000002-0000-0000-0000-000000000005", 24, 25),
            ("c6000002-0000-0000-0000-000000000006", 28, 29),
            ("c6000002-0000-0000-0000-000000000007", 32, 33),
            ("c6000002-0000-0000-0000-000000000008", 36, 37),
            ("c6000002-0000-0000-0000-000000000009", 38, 40),
        };

        foreach (var (id, weekStart, weekEnd) in milestoneTemplates)
        {
            builder.Entity<MotivationalTemplate>().HasData(new
            {
                Id = new Guid(id),
                Category = MotivationalCategory.Milestone,
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                IsActive = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate
            });
        }

        // ═══════════════════════════════════════════════════
        // TIP — Mẹo sức khỏe
        // ═══════════════════════════════════════════════════

        var tipTemplates = new (string id, int weekStart, int weekEnd)[]
        {
            ("c6000003-0000-0000-0000-000000000001", 0, 12),
            ("c6000003-0000-0000-0000-000000000002", 13, 27),
            ("c6000003-0000-0000-0000-000000000003", 28, 40),
        };

        foreach (var (id, weekStart, weekEnd) in tipTemplates)
        {
            builder.Entity<MotivationalTemplate>().HasData(new
            {
                Id = new Guid(id),
                Category = MotivationalCategory.Tip,
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                IsActive = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate
            });
        }

        // ═══════════════════════════════════════════════════
        // TRANSLATIONS — Vietnamese
        // ═══════════════════════════════════════════════════

        // Baby Size — VI
        var babySizeTranslationsVi = new (string templateId, string title, string message)[]
        {
            ("c6000001-0000-0000-0000-000000000001", "Bé to bằng hạt mè!", "Tuần 4-5: Bé mới chỉ nhỏ bằng hạt mè (0.1 cm), nhưng các cơ quan đã bắt đầu hình thành. Hãy bổ sung acid folic nhé mẹ!"),
            ("c6000001-0000-0000-0000-000000000002", "Bé to bằng hạt đậu lăng!", "Tuần 6-7: Bé dài khoảng 0.6 cm, tim bé đã bắt đầu đập. Mẹ có thể thấy nhịp tim bé qua siêu âm!"),
            ("c6000001-0000-0000-0000-000000000003", "Bé to bằng quả mâm xôi!", "Tuần 8-9: Bé dài 1.6 cm, các ngón tay bé đang hình thành. Mẹ nhớ uống đủ nước nhé!"),
            ("c6000001-0000-0000-0000-000000000004", "Bé to bằng quả mận!", "Tuần 10-11: Bé dài 3.1 cm, đã có thể cử động nhẹ. Giai đoạn này mẹ có thể bị ốm nghén nhiều."),
            ("c6000001-0000-0000-0000-000000000005", "Bé to bằng quả chanh!", "Tuần 12-13: Bé dài 5.4 cm, khuôn mặt bé đã rõ nét hơn. Mẹ sắp qua giai đoạn ốm nghén rồi!"),
            ("c6000001-0000-0000-0000-000000000006", "Bé to bằng quả cam!", "Tuần 14-15: Bé dài 8.7 cm, bé đã biết nhăn mặt và mút tay. Mẹ bắt đầu cảm thấy khỏe hơn!"),
            ("c6000001-0000-0000-0000-000000000007", "Bé to bằng quả bơ!", "Tuần 16-17: Bé dài 11.6 cm, xương bé đang cứng dần. Mẹ có thể bắt đầu cảm nhận bé đạp nhẹ!"),
            ("c6000001-0000-0000-0000-000000000008", "Bé to bằng quả xoài!", "Tuần 18-19: Bé dài 15.3 cm, bé đã biết nghe âm thanh. Hãy nói chuyện với bé mỗi ngày nhé!"),
            ("c6000001-0000-0000-0000-000000000009", "Bé to bằng quả chuối!", "Tuần 20-21: Bé dài 25.6 cm — nửa chặng đường rồi mẹ ơi! Bé đã có lông mày và mi mắt."),
            ("c6000001-0000-0000-0000-00000000000a", "Bé to bằng bắp ngô!", "Tuần 22-23: Bé dài 28.9 cm, da bé đang dần hồng hào hơn. Mẹ nhớ bổ sung sắt nhé!"),
            ("c6000001-0000-0000-0000-00000000000b", "Bé to bằng quả dưa lưới!", "Tuần 24-25: Bé dài khoảng 30 cm, phổi đang phát triển mạnh. Bé phản ứng với ánh sáng rồi mẹ ạ!"),
            ("c6000001-0000-0000-0000-00000000000c", "Bé to bằng bông cải xanh!", "Tuần 26-27: Bé dài 36.6 cm, mắt bé đã mở được. Bé đang tập thở trong bụng mẹ!"),
            ("c6000001-0000-0000-0000-00000000000d", "Bé to bằng quả bí ngô!", "Tuần 28-29: Bé dài 38.6 cm, nặng khoảng 1 kg. Não bé phát triển rất nhanh giai đoạn này!"),
            ("c6000001-0000-0000-0000-00000000000e", "Bé to bằng quả dừa!", "Tuần 30-31: Bé dài 40 cm, bé tích mỡ để giữ ấm sau khi sinh. Mẹ nên nghỉ ngơi nhiều hơn!"),
            ("c6000001-0000-0000-0000-00000000000f", "Bé to bằng quả dứa!", "Tuần 32-33: Bé dài 42.4 cm, xương bé gần như hoàn thiện. Mẹ bắt đầu chuẩn bị đồ sơ sinh nhé!"),
            ("c6000001-0000-0000-0000-000000000010", "Bé to bằng quả dưa!", "Tuần 34-35: Bé dài 45 cm, phổi gần trưởng thành. Mẹ nhớ đếm cử động bé hàng ngày!"),
            ("c6000001-0000-0000-0000-000000000011", "Bé to bằng quả bưởi!", "Tuần 36-37: Bé dài 47.4 cm, đầu bé đã quay xuống. Sắp được gặp con rồi mẹ ơi!"),
            ("c6000001-0000-0000-0000-000000000012", "Bé to bằng quả dưa hấu!", "Tuần 38-40: Bé dài khoảng 50 cm, nặng 3-3.5 kg. Bé đủ tháng và sẵn sàng chào đời!"),
        };

        foreach (var (templateId, title, message) in babySizeTranslationsVi)
        {
            builder.Entity<MotivationalTemplateTranslation>().HasData(new
            {
                TemplateId = new Guid(templateId),
                LanguageCode = "vi",
                Title = title,
                Message = message
            });
        }

        // Milestone — VI
        var milestoneTranslationsVi = new (string templateId, string title, string message)[]
        {
            ("c6000002-0000-0000-0000-000000000001", "Tim bé đập rồi! 💓", "Tuần 8: Tim bé đang đập 120-160 nhịp/phút, nhanh gấp đôi mẹ! Mẹ có thể nghe thấy qua siêu âm."),
            ("c6000002-0000-0000-0000-000000000002", "Bé biết nuốt! 🍼", "Tuần 12: Bé bắt đầu tập nuốt nước ối — đây là cách bé tập ăn trước khi ra đời!"),
            ("c6000002-0000-0000-0000-000000000003", "Bé biết đạp! 🦶", "Tuần 16: Mẹ bắt đầu cảm nhận bé cử động — những cú đạp đầu tiên thật tuyệt vời!"),
            ("c6000002-0000-0000-0000-000000000004", "Bé nghe được rồi! 👂", "Tuần 20: Bé đã nghe được giọng mẹ! Hãy hát và nói chuyện với bé nhiều nhé."),
            ("c6000002-0000-0000-0000-000000000005", "Phổi bé phát triển! 🫁", "Tuần 24: Phổi bé đang hình thành túi khí. Bé có thể sống ngoài tử cung nếu sinh non (với hỗ trợ y tế)."),
            ("c6000002-0000-0000-0000-000000000006", "Bé mở mắt! 👀", "Tuần 28: Bé đã mở mắt và nhìn thấy ánh sáng từ bên ngoài bụng mẹ!"),
            ("c6000002-0000-0000-0000-000000000007", "Bé quay đầu! 🔄", "Tuần 32: Hầu hết bé đã quay đầu xuống dưới, sẵn sàng cho ngày sinh."),
            ("c6000002-0000-0000-0000-000000000008", "Bé sẵn sàng! ✨", "Tuần 36: Bé đã phát triển gần hoàn thiện. Mẹ nên chuẩn bị túi đồ đi sinh nhé!"),
            ("c6000002-0000-0000-0000-000000000009", "Bé đủ tháng! 🎉", "Tuần 38-40: Bé đã sẵn sàng chào đời! Mẹ bình tĩnh và tin tưởng vào bản thân nhé."),
        };

        foreach (var (templateId, title, message) in milestoneTranslationsVi)
        {
            builder.Entity<MotivationalTemplateTranslation>().HasData(new
            {
                TemplateId = new Guid(templateId),
                LanguageCode = "vi",
                Title = title,
                Message = message
            });
        }

        // Tip — VI
        var tipTranslationsVi = new (string templateId, string title, string message)[]
        {
            ("c6000003-0000-0000-0000-000000000001", "Mẹo tam cá nguyệt 1 💊", "3 tháng đầu: Bổ sung acid folic 400mcg/ngày, ăn ít nhưng nhiều bữa để giảm ốm nghén, uống đủ 2L nước/ngày."),
            ("c6000003-0000-0000-0000-000000000002", "Mẹo tam cá nguyệt 2 🏃‍♀️", "3 tháng giữa: Giai đoạn mẹ khỏe nhất! Tập thể dục nhẹ (yoga, đi bộ), bổ sung sắt + canxi, theo dõi cân nặng đều đặn."),
            ("c6000003-0000-0000-0000-000000000003", "Mẹo tam cá nguyệt 3 🧸", "3 tháng cuối: Đếm cử động bé (>10 lần/ngày), chuẩn bị đồ sơ sinh, nghỉ ngơi nhiều, nằm nghiêng trái để tăng tuần hoàn."),
        };

        foreach (var (templateId, title, message) in tipTranslationsVi)
        {
            builder.Entity<MotivationalTemplateTranslation>().HasData(new
            {
                TemplateId = new Guid(templateId),
                LanguageCode = "vi",
                Title = title,
                Message = message
            });
        }

        // ═══════════════════════════════════════════════════
        // TRANSLATIONS — English
        // ═══════════════════════════════════════════════════

        // Baby Size — EN
        var babySizeTranslationsEn = new (string templateId, string title, string message)[]
        {
            ("c6000001-0000-0000-0000-000000000001", "Baby is the size of a poppy seed!", "Week 4-5: Baby is just 0.1 cm, but organs are starting to form. Remember to take your folic acid!"),
            ("c6000001-0000-0000-0000-000000000002", "Baby is the size of a lentil!", "Week 6-7: Baby is about 0.6 cm and the heart has started beating. You may see the heartbeat on ultrasound!"),
            ("c6000001-0000-0000-0000-000000000003", "Baby is the size of a raspberry!", "Week 8-9: Baby is 1.6 cm, fingers are forming. Stay hydrated!"),
            ("c6000001-0000-0000-0000-000000000004", "Baby is the size of a prune!", "Week 10-11: Baby is 3.1 cm and can make small movements. Morning sickness may peak around now."),
            ("c6000001-0000-0000-0000-000000000005", "Baby is the size of a lime!", "Week 12-13: Baby is 5.4 cm with more defined facial features. Morning sickness should ease soon!"),
            ("c6000001-0000-0000-0000-000000000006", "Baby is the size of an orange!", "Week 14-15: Baby is 8.7 cm — can squint, frown, and suck thumb. You should feel more energetic!"),
            ("c6000001-0000-0000-0000-000000000007", "Baby is the size of an avocado!", "Week 16-17: Baby is 11.6 cm, bones are hardening. You may start feeling first kicks!"),
            ("c6000001-0000-0000-0000-000000000008", "Baby is the size of a mango!", "Week 18-19: Baby is 15.3 cm and can hear sounds. Talk to your baby every day!"),
            ("c6000001-0000-0000-0000-000000000009", "Baby is the size of a banana!", "Week 20-21: Baby is 25.6 cm — halfway there! Baby now has eyebrows and eyelids."),
            ("c6000001-0000-0000-0000-00000000000a", "Baby is the size of an ear of corn!", "Week 22-23: Baby is 28.9 cm, skin is becoming more opaque. Remember to take your iron supplements!"),
            ("c6000001-0000-0000-0000-00000000000b", "Baby is the size of a cantaloupe!", "Week 24-25: Baby is about 30 cm, lungs are developing rapidly. Baby responds to light now!"),
            ("c6000001-0000-0000-0000-00000000000c", "Baby is the size of a broccoli!", "Week 26-27: Baby is 36.6 cm, eyes can open now. Baby is practicing breathing in the womb!"),
            ("c6000001-0000-0000-0000-00000000000d", "Baby is the size of a butternut squash!", "Week 28-29: Baby is 38.6 cm, weighing about 1 kg. Brain is developing very rapidly now!"),
            ("c6000001-0000-0000-0000-00000000000e", "Baby is the size of a coconut!", "Week 30-31: Baby is 40 cm, building up fat to stay warm after birth. Get more rest!"),
            ("c6000001-0000-0000-0000-00000000000f", "Baby is the size of a pineapple!", "Week 32-33: Baby is 42.4 cm, bones are nearly complete. Start preparing the nursery!"),
            ("c6000001-0000-0000-0000-000000000010", "Baby is the size of a honeydew melon!", "Week 34-35: Baby is 45 cm, lungs are nearly mature. Count baby movements daily!"),
            ("c6000001-0000-0000-0000-000000000011", "Baby is the size of a papaya!", "Week 36-37: Baby is 47.4 cm, head has turned down. Almost time to meet your baby!"),
            ("c6000001-0000-0000-0000-000000000012", "Baby is the size of a watermelon!", "Week 38-40: Baby is about 50 cm, weighing 3-3.5 kg. Baby is full-term and ready to be born!"),
        };

        foreach (var (templateId, title, message) in babySizeTranslationsEn)
        {
            builder.Entity<MotivationalTemplateTranslation>().HasData(new
            {
                TemplateId = new Guid(templateId),
                LanguageCode = "en",
                Title = title,
                Message = message
            });
        }

        // Milestone — EN
        var milestoneTranslationsEn = new (string templateId, string title, string message)[]
        {
            ("c6000002-0000-0000-0000-000000000001", "Baby's heart is beating! 💓", "Week 8: Baby's heart beats 120-160 bpm — twice as fast as yours! You can hear it via ultrasound."),
            ("c6000002-0000-0000-0000-000000000002", "Baby can swallow! 🍼", "Week 12: Baby begins swallowing amniotic fluid — it's how they practice eating before birth!"),
            ("c6000002-0000-0000-0000-000000000003", "Baby can kick! 🦶", "Week 16: You may start feeling baby's movements — those first kicks are magical!"),
            ("c6000002-0000-0000-0000-000000000004", "Baby can hear you! 👂", "Week 20: Baby can hear your voice! Sing and talk to your little one regularly."),
            ("c6000002-0000-0000-0000-000000000005", "Baby's lungs are developing! 🫁", "Week 24: Air sacs are forming in baby's lungs. Baby could survive outside the womb with medical support."),
            ("c6000002-0000-0000-0000-000000000006", "Baby opens eyes! 👀", "Week 28: Baby's eyes are open and can see light filtering through from outside!"),
            ("c6000002-0000-0000-0000-000000000007", "Baby turns head down! 🔄", "Week 32: Most babies have turned head-down, getting ready for delivery day."),
            ("c6000002-0000-0000-0000-000000000008", "Baby is almost ready! ✨", "Week 36: Baby is nearly fully developed. Start packing your hospital bag!"),
            ("c6000002-0000-0000-0000-000000000009", "Baby is full-term! 🎉", "Week 38-40: Baby is ready to be born! Stay calm and trust yourself."),
        };

        foreach (var (templateId, title, message) in milestoneTranslationsEn)
        {
            builder.Entity<MotivationalTemplateTranslation>().HasData(new
            {
                TemplateId = new Guid(templateId),
                LanguageCode = "en",
                Title = title,
                Message = message
            });
        }

        // Tip — EN
        var tipTranslationsEn = new (string templateId, string title, string message)[]
        {
            ("c6000003-0000-0000-0000-000000000001", "First trimester tips 💊", "Months 1-3: Take 400mcg folic acid daily, eat small frequent meals to reduce nausea, drink 2L water daily."),
            ("c6000003-0000-0000-0000-000000000002", "Second trimester tips 🏃‍♀️", "Months 4-6: Your most energetic period! Light exercise (yoga, walking), take iron & calcium, monitor weight regularly."),
            ("c6000003-0000-0000-0000-000000000003", "Third trimester tips 🧸", "Months 7-9: Count baby movements (10+/day), prepare baby essentials, rest well, sleep on your left side for better circulation."),
        };

        foreach (var (templateId, title, message) in tipTranslationsEn)
        {
            builder.Entity<MotivationalTemplateTranslation>().HasData(new
            {
                TemplateId = new Guid(templateId),
                LanguageCode = "en",
                Title = title,
                Message = message
            });
        }
    }
}
