using System.Collections.Generic;

namespace MyUmrah
{
    public enum Language { English = 0, Kreol = 1, Arabic = 2, Urdu = 3, French = 4 }

    /// <summary>
    /// Five-language foundation: English, Mauritian Creole, Arabic, Urdu, French.
    /// NOTE: translations are first drafts and MUST be reviewed by native speakers / qualified scholars before release.
    /// NOTE: Arabic/Urdu need an RTL shaping library + a font with those glyphs to render correctly (see docs/STATUS.md).
    /// </summary>
    public static class Loc
    {
        public static event System.Action Changed;
        public static Language Current { get; private set; }
        public static bool IsRtl { get { return Current == Language.Arabic || Current == Language.Urdu; } }

        public static readonly string[] EnglishNames = { "English", "Kreol Morisien", "Arabic", "Urdu", "Francais" };

        static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>();

        static Loc() { Register(); }

        public static void Set(Language l) { Current = l; if (Changed != null) Changed(); }
        public static void ResetEvents() { Changed = null; }
        public static IEnumerable<string> Keys { get { return table.Keys; } }
        public static string[] Raw(string key) { string[] a; return table.TryGetValue(key, out a) ? a : null; }

        public static string T(string key)
        {
            string[] a;
            if (!table.TryGetValue(key, out a)) return key;
            string s = a[(int)Current];
            return string.IsNullOrEmpty(s) ? a[0] : s;
        }

        public static string T(string key, params object[] args) { return string.Format(T(key), args); }

        static void A(string k, string en, string kr, string ar, string ur, string fr)
        {
            table[k] = new[] { en, kr, ar, ur, fr };
        }

        static void Register()
        {
            // key, English, Kreol, Arabic, Urdu, French
            A("title", "My Umrah Journey", "Mo Vwayaz Omra", "رحلتي إلى العمرة", "میرا عمرہ کا سفر", "Mon Voyage de la Omra");
            A("play", "Play", "Zouwe", "ابدأ", "کھیلیں", "Jouer");
            A("continue", "Continue", "Kontinye", "متابعة", "جاری رکھیں", "Continuer");
            A("settings", "Settings", "Paramet", "الإعدادات", "ترتیبات", "Paramètres");
            A("language", "Language", "Lang", "اللغة", "زبان", "Langue");
            A("quality", "Graphics quality", "Kalite grafik", "جودة الرسوميات", "گرافکس کا معیار", "Qualité graphique");
            A("quit", "Quit", "Kit", "خروج", "باہر نکلیں", "Quitter");
            A("resume", "Resume", "Re-pran", "استئناف", "دوبارہ شروع", "Reprendre");
            A("save", "Save game", "Sovgard", "حفظ اللعبة", "گیم محفوظ کریں", "Sauvegarder");
            A("saved", "Game saved", "Zwe sovgarde", "تم حفظ اللعبة", "گیم محفوظ ہو گیا", "Partie sauvegardée");
            A("missions", "Missions", "Misyon", "المهام", "مشن", "Missions");
            A("inventory", "Inventory", "Sak", "الحقيبة", "سامان", "Inventaire");
            A("close", "Close", "Ferm", "إغلاق", "بند کریں", "Fermer");
            A("camera", "View", "Kamera", "الكاميرا", "کیمرہ", "Vue");
            A("interact", "Interact", "Interaz", "تفاعل", "عمل", "Agir");
            A("enter_vehicle", "Enter", "Monte", "ركوب", "سوار ہوں", "Monter");
            A("exit_vehicle", "Exit", "Desann", "نزول", "اتریں", "Descendre");
            A("jump", "Jump", "Sote", "قفز", "کودیں", "Sauter");
            A("run", "Run", "Kouri", "جري", "دوڑیں", "Courir");
            A("accel", "Gas", "Akselere", "تسارع", "رفتار", "Accélérer");
            A("brake", "Brake", "Fren", "فرامل", "بریک", "Frein");
            A("menu", "Menu", "Menu", "القائمة", "مینو", "Menu");
            A("talk", "Talk", "Koz", "تحدّث", "بات کریں", "Parler");
            A("pickup", "Pick up", "Pran", "التقاط", "اٹھائیں", "Ramasser");
            A("learn", "Learn", "Aprann", "تعلّم", "سیکھیں", "Découvrir");
            A("refuel", "Refuel", "Plen rezervwar", "تعبئة الوقود", "ایندھن بھریں", "Faire le plein");
            A("refueled", "Tank full", "Rezervwar plen", "تم ملء الخزان", "ٹینک بھر گیا", "Réservoir plein");
            A("no_money", "Not enough money", "Pa ena ase larzan", "لا يوجد مال كافٍ", "کافی رقم نہیں", "Pas assez d'argent");
            A("out_of_fuel", "Out of fuel!", "Pena plis esans!", "نفد الوقود!", "ایندھن ختم!", "Plus de carburant !");
            A("no_vehicle_near", "No vehicle nearby", "Pena okenn veikil pre", "لا توجد مركبة قريبة", "قریب کوئی گاڑی نہیں", "Aucun véhicule à proximité");
            A("picked", "Picked up: {0}", "Finn pran: {0}", "تم التقاط: {0}", "اٹھایا: {0}", "Ramassé : {0}");
            A("item_prayer_mat", "Prayer mat", "Tapi lapriyer", "سجادة الصلاة", "جائے نماز", "Tapis de prière");
            A("empty_items", "No items yet", "Pena nanye ankor", "لا توجد عناصر بعد", "ابھی کوئی چیز نہیں", "Aucun objet pour l'instant");
            A("mission_complete", "Mission complete!", "Misyon fini!", "اكتملت المهمة!", "مشن مکمل!", "Mission accomplie !");
            A("reward", "Reward: Rs {0}", "Rekonpans: Rs {0}", "المكافأة: Rs {0}", "انعام: Rs {0}", "Récompense : Rs {0}");
            A("loading", "Loading Castel...", "Pe sarze Castel...", "جارٍ تحميل كاستل...", "کاسل لوڈ ہو رہا ہے...", "Chargement de Castel...");
            A("q_low", "Low", "Ba", "منخفضة", "کم", "Basse");
            A("q_med", "Medium", "Mwayen", "متوسطة", "درمیانی", "Moyenne");
            A("q_high", "High", "Ot", "عالية", "اعلیٰ", "Haute");
            A("fuel", "Fuel", "Esans", "الوقود", "ایندھن", "Carburant");
            A("veh_car", "Car", "Loto", "سيارة", "گاڑی", "Voiture");
            A("veh_taxi", "Taxi", "Taxi", "سيارة أجرة", "ٹیکسی", "Taxi");
            A("veh_bus", "Bus", "Bis", "حافلة", "بس", "Bus");
            A("veh_bike", "Motorcycle", "Motosiklet", "دراجة نارية", "موٹر سائیکل", "Moto");
            A("controls_hint", "Left side: move   Right side: look   Buttons: jump, run, interact, view", "Gos: bouze   Droite: gete   Bouton: sote, kouri, interaz, kamera", "اليسار: حركة   اليمين: نظر   الأزرار: قفز، جري، تفاعل، كاميرا", "بائیں: حرکت   دائیں: دیکھیں   بٹن: کودیں، دوڑیں، عمل، کیمرہ", "Gauche : bouger   Droite : regarder   Boutons : sauter, courir, agir, vue");
            A("masjid_name", "Sheikh Abdul Qadir Jeelani R.A Masjid Castel", "Sheikh Abdul Qadir Jeelani R.A Masjid Castel", "Sheikh Abdul Qadir Jeelani R.A Masjid Castel", "Sheikh Abdul Qadir Jeelani R.A Masjid Castel", "Sheikh Abdul Qadir Jeelani R.A Masjid Castel");
            A("masjid_info", "A place of prayer and learning in Castel, Vacoas-Phoenix. This 3D model is a stylised placeholder, not an accurate reproduction. Detailed architecture and interior will be added in a later phase.", "Enn landrwa pou priye ek aprann dan Castel, Vakoa-Fenix. Sa model 3D la enn plas pou lamem, pa enn kopi egzak. Arsitekti an detay ek andedan pou azoute pli tar.", "مكان للصلاة والتعلّم في كاستل، فاكواس-فينيكس. هذا النموذج ثلاثي الأبعاد مؤقت ومبسّط وليس نسخة دقيقة. ستُضاف العمارة التفصيلية والداخل في مرحلة لاحقة.", "کاسل، ویکواس-فینکس میں عبادت اور تعلیم کی جگہ۔ یہ 3D ماڈل ایک عارضی سادہ نمونہ ہے، درست نقل نہیں۔ تفصیلی تعمیر اور اندرونی حصہ بعد کے مرحلے میں شامل کیا جائے گا۔", "Un lieu de prière et d'apprentissage à Castel, Vacoas-Phoenix. Ce modèle 3D est un substitut stylisé, pas une reproduction fidèle. L'architecture détaillée et l'intérieur seront ajoutés plus tard.");
            A("npc_1", "Assalamu alaikum! Welcome to Castel.", "Assalamu alaikum! Byenveni dan Castel.", "السلام عليكم! أهلاً بك في كاستل.", "السلام علیکم! کاسل میں خوش آمدید۔", "Assalamu alaikum ! Bienvenue à Castel.");
            A("npc_2", "Have you visited the Masjid yet? It is just ahead.", "Ou finn al lamosker deja? Li la devan.", "هل زرت المسجد بعد؟ إنه أمامك مباشرة.", "کیا آپ مسجد گئے؟ وہ بالکل آگے ہے۔", "Avez-vous déjà visité la mosquée ? Elle est juste devant.");
            A("npc_3", "Be careful on the road - we drive on the left in Mauritius.", "Atansyon lor semin - nou kondir agos dan Moris.", "انتبه على الطريق — نقود على الجانب الأيسر في موريشيوس.", "سڑک پر احتیاط کریں — ماریشس میں ہم بائیں جانب چلتے ہیں۔", "Faites attention sur la route - on roule à gauche à Maurice.");
            A("m_walk_t", "Take a walk", "Fer enn ti promnad", "تمشَّ قليلاً", "ذرا چہل قدمی کریں", "Faire une promenade");
            A("m_walk_d", "Walk 50 metres around Castel.", "Marse 50 met dan Castel.", "امشِ 50 مترًا في كاستل.", "کاسل میں 50 میٹر پیدل چلیں۔", "Marchez 50 mètres dans Castel.");
            A("m_cam_t", "Change your view", "Sanze to vizyon", "غيّر زاوية الرؤية", "اپنا زاویہ بدلیں", "Changez de vue");
            A("m_cam_d", "Switch between first-person and third-person camera.", "Sanze ant kamera premye personn ek trwazyem personn.", "بدّل بين منظور الشخص الأول ومنظور الشخص الثالث.", "پہلے شخص اور تیسرے شخص کے کیمرے کے درمیان تبدیل کریں۔", "Passez de la vue à la première personne à la troisième personne.");
            A("m_mosque_t", "Visit the Masjid", "Vizit Mosker", "زُر المسجد", "مسجد کی زیارت کریں", "Visiter la mosquée");
            A("m_mosque_d", "Go to Sheikh Abdul Qadir Jeelani R.A Masjid Castel and learn about it.", "Ale kot Sheikh Abdul Qadir Jeelani R.A Masjid Castel ek aprann lor li.", "اذهب إلى مسجد الشيخ عبد القادر الجيلاني رحمه الله في كاستل وتعرّف عليه.", "شیخ عبدالقادر جیلانی رحمہ اللہ مسجد، کاسل جائیں اور اس کے بارے میں جانیں۔", "Rendez-vous à la mosquée Sheikh Abdul Qadir Jeelani R.A de Castel et découvrez-la.");
            A("m_drive_t", "Take a drive", "Fer enn ti tour", "قُد مركبة", "گاڑی چلائیں", "Faire un tour en voiture");
            A("m_drive_d", "Get in a vehicle and drive 150 metres.", "Monte dan enn veikil ek kondir 150 met.", "اركب مركبة وقُد مسافة 150 مترًا.", "گاڑی میں بیٹھیں اور 150 میٹر چلائیں۔", "Montez dans un véhicule et conduisez 150 mètres.");
            A("m_greet_t", "Greet a neighbour", "Salie enn vwazin", "سلّم على جار", "پڑوسی کو سلام کریں", "Saluer un voisin");
            A("m_greet_d", "Say Salaam to someone in the village.", "Fer Salaam enn dimoun dan vilaz.", "ألقِ السلام على أحد في القرية.", "گاؤں میں کسی کو سلام کریں۔", "Dites Salaam à quelqu'un du village.");
            A("m_swim_t", "Cool off", "Rafres twa", "انتعش", "ٹھنڈک حاصل کریں", "Se rafraîchir");
            A("m_swim_d", "Swim in the lake for 5 seconds.", "Naze dan lak pou 5 segonn.", "اسبح في البحيرة لمدة 5 ثوانٍ.", "جھیل میں 5 سیکنڈ تیریں۔", "Nagez dans le lac pendant 5 secondes.");
        }
    }
}
