# Feature 008 — Source Display Metadata Review Report

Generated UTC: 2026-06-15T09:06:26Z

## 1. Verdict

**Review overlay created and validated.** A non-destructive display/source-metadata overlay was
generated for **all 167** approved translation sources, keyed 1:1 to `manifest.json`. The manifest
remains the frozen file/hash/source contract — it was **not** modified. Recommended verdict:
**`READY_FOR_SPEC_KIT_WITH_METADATA_REVIEW`**.

## 2. Scope

Metadata curation only. Improves filename-derived English display names and proposes Arabic
display/translator names where confidently transliterable, and flags everything uncertain for human
review. No raw sources, copied `sources/` files, manifest, or backend code were touched.

## 3. Input files

- `resources/import-sources/quran-translations/manifest.json` (record set — source of truth)
- `resources/import-sources/quran-translations/package-report.md`
- `resources/import-sources/quran-translations/README.md`
- `docs/feature-008-quran-translations-foundation/feature-008-decisions-addendum.md`
- `docs/feature-008-quran-translations-foundation/feature-008-quran-translations-foundation-planning-report.md`

## 4. Output file

- `resources/import-sources/quran-translations/source-display-metadata.review.json` (this overlay)

## 5. Total records

**167** — exactly one per manifest source (no extras, none missing; no word-by-word, no
excluded sources).

## 6. Count by confidence

| Confidence | Count | Meaning |
|---|---|---|
| `high` | 16 | Verified English + Arabic; no review needed |
| `medium` | 142 | Plausible name; Arabic proposed or null — verify |
| `low` | 9 | Unknown / generic / machine-derived |
| **Total** | **167** | |

## 7. Count needing review

**151** of 167 records have `needsReview = true`
(142 medium + 9 low). The 16 high-confidence
records need no review.

## 8. Verified (high-confidence, no review) — 16 records

| sourceKey | Language | displayNameEn | displayNameAr |
|---|---|---|---|
| `bn-dr-abu-bakr-muhammad-zakaria` | Bengali | Dr. Abu Bakr Muhammad Zakaria | د. أبو بكر محمد زكريا |
| `en-al-maududi` | English | Abul A'la Maududi | أبو الأعلى المودودي |
| `en-asad` | English | Muhammad Asad | محمد أسد |
| `en-ghali` | English | Muhammad Mahmoud Ghali | محمد محمود غالي |
| `en-maulana-wahiduddin-khan` | English | Maulana Wahiduddin Khan | وحيد الدين خان |
| `en-muhsin-khan` | English | Muhammad Muhsin Khan | محمد محسن خان |
| `en-sahih-international` | English | Saheeh International | صحيح إنترناشونال |
| `en-taqi-ud-din-al-hilali-muhsin-khan` | English | Al-Hilali & Muhsin Khan | تقي الدين الهلالي ومحمد محسن خان |
| `en-taqi-usmani` | English | Mufti Taqi Usmani | محمد تقي العثماني |
| `en-yusufali` | English | Abdullah Yusuf Ali | عبد الله يوسف علي |
| `fr-hamidullah` | French | Muhammad Hamidullah | محمد حميد الله |
| `id-king-fahad-quran-complex` | Indonesian | King Fahd Glorious Quran Printing Complex | مجمع الملك فهد لطباعة المصحف الشريف |
| `pt-helmi-nasr` | Portuguese | Helmi Nasr | حلمي نصر |
| `ru-kuliev` | Russian | Elmir Kuliev | إلمير كولييف |
| `ur-al-maududi` | Urdu | Abul A'la Maududi | أبو الأعلى المودودي |
| `uz-muhammad-sodik-muhammad-yusuf` | Uzbek | Muhammad Sodiq Muhammad Yusuf | محمد صادق محمد يوسف |

## 9. Records needing review (151)

Sorted low-confidence first, then medium. `proposed displayNameAr` of `—` means Arabic was left
`null` pending human input.

| sourceKey | packageFile | Language | Type | Conf. | Current displayNameEn | Proposed displayNameAr | Review reasons |
|---|---|---|---|---|---|---|---|
| `az-unknown` | `az-unknown.json` | Azerbaijani | simple | low | Azerbaijani (unattributed) | — | unattributed/unknown |
| `bs-unknown` | `bs-unknown.json` | Bosnian | simple | low | Bosnian (unattributed) | — | unattributed/unknown |
| `cs-unknown` | `cs-unknown.json` | Czech | simple | low | Czech (unattributed) | — | unattributed/unknown; lang name best-effort |
| `dv-unknow` | `dv-unknow.json` | Divehi | simple | low | Divehi (unattributed) | — | unattributed/unknown; filename typo; lang name best-effort |
| `fi-unknown` | `fi-unknown.json` | Finnish | simple | low | Finnish (unattributed) | — | unattributed/unknown; lang name best-effort |
| `id-id` | `id-id.fn.json` | Indonesian | with_footnotes | low | Indonesian translation (source unspecified) | — | machine-derived key |
| `mrw-mrn-unknown` | `mrw-mrn-unknown.json` | Maranao | simple | low | Maranao (unattributed) | — | unattributed/unknown; filename typo; lang name best-effort |
| `no-unknown` | `no-unknown.json` | Norwegian | simple | low | Norwegian (unattributed) | — | unattributed/unknown; lang name best-effort |
| `tt-unknow` | `tt-unknow.json` | Tatar | simple | low | Tatar (unattributed) | — | unattributed/unknown; filename typo; lang name best-effort |
| `ak-rowad-translation-center` | `ak-rowad-translation-center.json` | Akan (Asante Twi) | simple | medium | Rowad Translation Center | — | EN from filename; AR unverified; Rowwad spelling variant; generic/work-title name; lang name best-effort |
| `am-amharic-translation-zain` | `am-amharic-translation-zain.json` | Amharic | simple | medium | Amharic Translation Zain | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `am-sadiq` | `am-sadiq.json` | Amharic | simple | medium | Sadiq | — | EN from filename; AR unverified; lang name best-effort |
| `as-shaykh-rafeequl-islam-habibur-rahman` | `as-shaykh-rafeequl-islam-habibur-rahman.fn.json` | Assamese | with_footnotes | medium | Shaykh Rafeequl Islam & Habibur Rahman | الشيخ رفيق الإسلام وحبيب الرحمن | verify AR translit |
| `az-alikhan` | `az-alikhan.json` | Azerbaijani | simple | medium | Alikhan | — | EN from filename; AR unverified |
| `ber-ramdane-at-mansour` | `ber-ramdane-at-mansour.json` | Amazigh (Berber) | simple | medium | Ramdane at Mansour | — | EN from filename; AR unverified; lang name best-effort |
| `bg-bulgarian-translation` | `bg-bulgarian-translation.json` | Bulgarian | simple | medium | Bulgarian Translation | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `bg-tzvetan-theophanov` | `bg-tzvetan-theophanov.json` | Bulgarian | simple | medium | Tzvetan Theophanov | — | EN from filename; AR unverified; lang name best-effort |
| `bm-baba-mamady-jani` | `bm-baba-mamady-jani.json` | Bambara | simple | medium | Baba Mamady Jani | — | EN from filename; AR unverified; lang name best-effort |
| `bm-suliman-kanti` | `bm-suliman-kanti.json` | Bambara | simple | medium | Suliman Kanti | — | EN from filename; AR unverified; lang name best-effort |
| `bn-fathul-majid-bn` | `bn-fathul-majid-bn.json` | Bengali | simple | medium | Fathul Majid Bn | — | EN from filename; AR unverified |
| `bn-rawai-al-bayan` | `bn-rawai-al-bayan.json` | Bengali | simple | medium | Rawai al Bayan | — | EN from filename; AR unverified |
| `bn-sheikh-mujibur-rahman` | `bn-sheikh-mujibur-rahman.json` | Bengali | simple | medium | Sheikh Mujibur Rahman | الشيخ مجيب الرحمن | verify AR translit |
| `bn-taisirul-quran` | `bn-taisirul-quran.json` | Bengali | simple | medium | Taisirul Quran | — | EN from filename; AR unverified; generic/work-title name |
| `bs-besim-korkut` | `bs-besim-korkut.json` | Bosnian | simple | medium | Besim Korkut | — | EN from filename; AR unverified |
| `bs-dar-al-salam-center` | `bs-dar-al-salam-center.json` | Bosnian | simple | medium | Dar Al-Salam Center | مركز دار السلام | org/publisher attribution |
| `ceb-bisayan-translation-rowwad-center` | `ceb-bisayan-translation-rowwad-center.json` | Cebuano (Bisaya) | simple | medium | Bisayan Translation Rowwad Center | — | EN from filename; AR unverified; Rowwad spelling variant; generic/work-title name; lang name best-effort |
| `de-abu-reda-muhammad-ibn-ahmad` | `de-abu-reda-muhammad-ibn-ahmad.json` | German | simple | medium | Abu Rida Muhammad ibn Ahmad ibn Rassoul | أبو رضا محمد بن أحمد بن رسول | verify AR translit; lang name best-effort |
| `de-bubenheim` | `de-bubenheim.json` | German | simple | medium | Bubenheim | — | EN from filename; AR unverified; lang name best-effort |
| `dv-ml-shaikh-aboobakr-ibrahim-ali` | `dv-ml-shaikh-aboobakr-ibrahim-ali.fn.json` | Divehi | with_footnotes | medium | Ml Shaikh Aboobakr Ibrahim Ali | — | EN from filename; AR unverified; lang name best-effort |
| `el-greek-translation` | `el-greek-translation.json` | Greek | simple | medium | Greek Translation | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `en-arberry` | `en-arberry.json` | English | simple | medium | Arberry | — | EN from filename; AR unverified |
| `en-bridges-translation` | `en-bridges-translation.fn.json` | English | with_footnotes | medium | Bridges Translation | — | EN from filename; AR unverified; generic/work-title name |
| `en-daryabadi` | `en-daryabadi.json` | English | simple | medium | Abdul Majid Daryabadi | عبد الماجد الدريابادي | verify AR translit |
| `en-dr-t-b-irving` | `en-dr-t-b-irving.json` | English | simple | medium | Dr. T B Irving | — | EN from filename; AR unverified |
| `en-haleem` | `en-haleem.fn.json` | English | with_footnotes | medium | M. A. S. Abdel Haleem | محمد عبد الحليم | verify AR translit |
| `en-pickthall` | `en-pickthall.json` | English | simple | medium | Pickthall | — | EN from filename; AR unverified |
| `en-qaribullah` | `en-qaribullah.json` | English | simple | medium | Hasan al-Fatih Qaribullah | حسن الفاتح قريب الله | verify AR translit |
| `en-ruwwad-center` | `en-ruwwad-center.json` | English | simple | medium | Ruwwad Center | — | EN from filename; AR unverified; Rowwad spelling variant |
| `en-sarwar` | `en-sarwar.json` | English | simple | medium | Muhammad Sarwar | محمد سرور | verify AR translit |
| `en-shakir` | `en-shakir.json` | English | simple | medium | Muhammad Habib Shakir | محمد حبيب شاكر | verify AR translit |
| `es-cortes` | `es-cortes.json` | Spanish | simple | medium | Cortes | — | EN from filename; AR unverified |
| `es-isa-garcia` | `es-isa-garcia.fn.json` | Spanish | with_footnotes | medium | Isa Garcia | عيسى غارسيا | verify AR translit |
| `es-montada-islamic-foundation` | `es-montada-islamic-foundation.fn.json` | Spanish | with_footnotes | medium | Montada Islamic Foundation | مؤسسة المنتدى الإسلامي | org/publisher attribution |
| `es-noor-international-center` | `es-noor-international-center.fn.json` | Spanish | with_footnotes | medium | Noor International Center | مركز نور إنترناشونال | org/publisher attribution |
| `fa-fr-hussein-taji` | `fa-fr-hussein-taji.json` | Persian | simple | medium | Hussein Taji Kal Dari | حسين تاجي كل دري | verify AR translit |
| `fa-islamhouse-com` | `fa-islamhouse-com.json` | Persian | simple | medium | IslamHouse.com | موقع دار الإسلام (IslamHouse.com) | org/publisher attribution |
| `ff-rowad-translation-center` | `ff-rowad-translation-center.json` | Fulah | simple | medium | Rowad Translation Center | — | EN from filename; AR unverified; Rowwad spelling variant; generic/work-title name |
| `fil-filipino-iranionian-translation` | `fil-filipino-iranionian-translation.json` | Filipino | simple | medium | Filipino Iranionian Translation | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `fr-montada-islamic-foundation` | `fr-montada-islamic-foundation.fn.json` | French | with_footnotes | medium | Montada Islamic Foundation | مؤسسة المنتدى الإسلامي | org/publisher attribution |
| `fr-rashid-maash` | `fr-rashid-maash.fn.json` | French | with_footnotes | medium | Rashid Maash | رشيد معاش | verify AR translit |
| `gu-rabila-al-umry` | `gu-rabila-al-umry.json` | Gujarati | simple | medium | Rabila al-Umry | رابيلا العمري | verify AR translit; lang name best-effort |
| `ha-abubakar` | `ha-abubakar.json` | Hausa | simple | medium | Abubakar | — | EN from filename; AR unverified; lang name best-effort |
| `ha-abubakar-mahmood-jummi` | `ha-abubakar-mahmood-jummi.fn.json` | Hausa | with_footnotes | medium | Abubakar Mahmood Jummi | — | EN from filename; AR unverified; lang name best-effort |
| `hi-maulana-azizul-haque-al-umari` | `hi-maulana-azizul-haque-al-umari.fn.json` | Hindi | with_footnotes | medium | Maulana Azizul Haque al-Umari | عزيز الحق العمري | verify AR translit |
| `hr-croatian-translation-rwwad` | `hr-croatian-translation-rwwad.json` | Croatian | simple | medium | Croatian Translation Rwwad | — | EN from filename; AR unverified; Rowwad spelling variant; generic/work-title name; lang name best-effort |
| `id-the-sabiq-company` | `id-the-sabiq-company.fn.json` | Indonesian | with_footnotes | medium | The Sabiq Company | — | EN from filename; AR unverified |
| `it-hamza-roberto-piccardo` | `it-hamza-roberto-piccardo.fn.json` | Italian | with_footnotes | medium | Hamza Roberto Piccardo | حمزة روبرتو بيكاردو | verify AR translit |
| `it-othman-al-sharif` | `it-othman-al-sharif.json` | Italian | simple | medium | Othman al-Sharif | عثمان الشريف | verify AR translit |
| `ja-ryoichi-mita` | `ja-ryoichi-mita.json` | Japanese | simple | medium | Ryoichi Mita | — | EN from filename; AR unverified |
| `ja-saeed-sato` | `ja-saeed-sato.json` | Japanese | simple | medium | Saeed Sato | — | EN from filename; AR unverified |
| `kk-khalifa-altay` | `kk-khalifa-altay.json` | Kazakh | simple | medium | Khalifa Altay | خليفة آلتاي | verify AR translit; lang name best-effort |
| `km-cambodian-muslim-community-development` | `km-cambodian-muslim-community-development.json` | Khmer | simple | medium | Cambodian Muslim Community Development | تنمية المجتمع المسلم الكمبودي | org/publisher attribution |
| `km-khmer-translation-rwwad-translation-center` | `km-khmer-translation-rwwad-translation-center.json` | Khmer | simple | medium | Khmer Translation Rwwad Translation Center | — | EN from filename; AR unverified; Rowwad spelling variant; generic/work-title name |
| `kn-kannada-translation-bashir-missouri` | `kn-kannada-translation-bashir-missouri.json` | Kannada | simple | medium | Kannada Translation Bashir Missouri | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `ko-hamed-choi` | `ko-hamed-choi.json` | Korean | simple | medium | Hamed Choi | حامد تشوي | verify AR translit; lang name best-effort |
| `ks-bayanul-furqan-koshur-quran` | `ks-bayanul-furqan-koshur-quran.json` | Kashmiri | simple | medium | Bayanul Furqan Koshur Quran | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `ku-kurdish-kurmanji-translation` | `ku-kurdish-kurmanji-translation.json` | Kurdish | simple | medium | Kurdish Kurmanji Translation | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `ku-kurdish-translation-salahuddin` | `ku-kurdish-translation-salahuddin.json` | Kurdish | simple | medium | Kurdish Translation Salahuddin | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `ku-muhammad-saleh-bamoki` | `ku-muhammad-saleh-bamoki.json` | Kurdish | simple | medium | Muhammad Saleh Bamoki | — | EN from filename; AR unverified; lang name best-effort |
| `ky-kyrgyz-hakimov` | `ky-kyrgyz-hakimov.json` | Kyrgyz | simple | medium | Kyrgyz Hakimov | — | EN from filename; AR unverified |
| `ln-lingala-translation` | `ln-lingala-translation.json` | Lingala | simple | medium | Lingala Translation | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `lt-lithuanian-translation` | `lt-lithuanian-translation.fn.json` | Lithuanian | with_footnotes | medium | Lithuanian Translation | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `luy-luhya-translation` | `luy-luhya-translation.json` | Luhya | simple | medium | Luhya Translation | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `mdh-maguindanao-rwwad` | `mdh-maguindanao-rwwad.json` | Maguindanao | simple | medium | Maguindanao Rwwad | — | EN from filename; AR unverified; Rowwad spelling variant; lang name best-effort |
| `mg-malagasy-translation-rowad` | `mg-malagasy-translation-rowad.json` | Malagasy | simple | medium | Malagasy Translation Rowad | — | EN from filename; AR unverified; Rowwad spelling variant; generic/work-title name; lang name best-effort |
| `mk-macedonian-scholars` | `mk-macedonian-scholars.fn.json` | Macedonian | with_footnotes | medium | Macedonian Scholars | علماء مقدونيا | org/publisher attribution; lang name best-effort |
| `ml-abdul-hameed` | `ml-abdul-hameed.json` | Malayalam | simple | medium | Abdul Hameed | — | EN from filename; AR unverified |
| `ml-abdul-hamid-haidar-kanhi-muhammad` | `ml-abdul-hamid-haidar-kanhi-muhammad.json` | Malayalam | simple | medium | Abdul Hamid Haidar Kanhi Muhammad | — | EN from filename; AR unverified |
| `ml-karakunnu` | `ml-karakunnu.json` | Malayalam | simple | medium | Karakunnu | — | EN from filename; AR unverified |
| `mos-moore-rwwad` | `mos-moore-rwwad.json` | Mossi (Mooré) | simple | medium | Moore Rwwad | — | EN from filename; AR unverified; Rowwad spelling variant; lang name best-effort |
| `mr-muhammad-shafi-i-ansari` | `mr-muhammad-shafi-i-ansari.json` | Marathi | simple | medium | Muhammad Shafi Ansari | محمد شفيع الأنصاري | verify AR translit; lang name best-effort |
| `ms-abdullah-basamia` | `ms-abdullah-basamia.json` | Malay | simple | medium | Abdullah Basamia | — | EN from filename; AR unverified; lang name best-effort |
| `mt-qoran-imqaddes` | `mt-qoran-imqaddes.json` | Maltese | simple | medium | Qoran Imqaddes | — | EN from filename; AR unverified; lang name best-effort |
| `ne-ahl-al-hadith-central-society-of-nepal` | `ne-ahl-al-hadith-central-society-of-nepal.json` | Nepali | simple | medium | Ahl al-Hadith Central Society of Nepal | جمعية أهل الحديث المركزية في نيبال | org/publisher attribution; lang name best-effort |
| `nl-dutch-islamic-center` | `nl-dutch-islamic-center.json` | Dutch | simple | medium | Dutch Islamic Center | المركز الإسلامي الهولندي | org/publisher attribution; lang name best-effort |
| `nl-sofian-s-siregar` | `nl-sofian-s-siregar.json` | Dutch | simple | medium | Sofian S Siregar | — | EN from filename; AR unverified; lang name best-effort |
| `ny-chewa-translation` | `ny-chewa-translation.json` | Chichewa (Nyanja) | simple | medium | Chewa Translation | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `om-ghali-apapur-apaghuna` | `om-ghali-apapur-apaghuna.json` | Oromo | simple | medium | Ghali Apapur Apaghuna | — | EN from filename; AR unverified; lang name best-effort |
| `pa-punjabi-arif` | `pa-punjabi-arif.json` | Punjabi | simple | medium | Punjabi Arif | — | EN from filename; AR unverified; lang name best-effort |
| `pl-jozef-bielawski` | `pl-jozef-bielawski.json` | Polish | simple | medium | Jozef Bielawski | — | EN from filename; AR unverified; lang name best-effort |
| `prs-mawlawi-muhammad-anwar-badkhashani` | `prs-mawlawi-muhammad-anwar-badkhashani.json` | Dari | simple | medium | Mawlawi Muhammad Anwar Badakhshani | المولوي محمد أنور البدخشاني | verify AR translit; lang name best-effort |
| `ps-pashto-sarfaraz` | `ps-pashto-sarfaraz.json` | Pashto | simple | medium | Pashto Sarfaraz | — | EN from filename; AR unverified |
| `ps-pashto-translation-rowwad-translation-center` | `ps-pashto-translation-rowwad-translation-center.json` | Pashto | simple | medium | Pashto Translation Rowwad Translation Center | — | EN from filename; AR unverified; Rowwad spelling variant; generic/work-title name |
| `ps-zakaria-abulsalam` | `ps-zakaria-abulsalam.json` | Pashto | simple | medium | Zakaria Abulsalam | — | EN from filename; AR unverified |
| `pt-samir` | `pt-samir.json` | Portuguese | simple | medium | Samir | — | EN from filename; AR unverified; lang name best-effort |
| `rn-ikirundi-gehiti` | `rn-ikirundi-gehiti.json` | Kirundi | simple | medium | Ikirundi Gehiti | — | EN from filename; AR unverified; lang name best-effort |
| `ro-grigore` | `ro-grigore.json` | Romanian | simple | medium | Grigore | — | EN from filename; AR unverified; lang name best-effort |
| `ro-islamic-and-cultural-league` | `ro-islamic-and-cultural-league.fn.json` | Romanian | with_footnotes | medium | Islamic and Cultural League | الرابطة الإسلامية والثقافية | org/publisher attribution; lang name best-effort |
| `ro-romanian-translation` | `ro-romanian-translation.json` | Romanian | simple | medium | Romanian Translation | — | EN from filename; AR unverified; generic/work-title name; lang name best-effort |
| `ru-abu-adel` | `ru-abu-adel.fn.json` | Russian | with_footnotes | medium | Abu Adel | أبو عادل | verify AR translit |
| `ru-gordy` | `ru-gordy.json` | Russian | simple | medium | Gordy | — | EN from filename; AR unverified |
| `ru-ministry-of-awqaf` | `ru-ministry-of-awqaf.fn.json` | Russian | with_footnotes | medium | Ministry of Awqaf | — | EN from filename; AR unverified |
| `ru-nuri` | `ru-nuri.json` | Russian | simple | medium | Nuri | — | EN from filename; AR unverified |
| `ru-russian-translation-aboadel` | `ru-russian-translation-aboadel.json` | Russian | simple | medium | Russian Translation Aboadel | — | EN from filename; AR unverified; generic/work-title name |
| `rw-the-rwanda-muslims-association-team` | `rw-the-rwanda-muslims-association-team.fn.json` | Kinyarwanda | with_footnotes | medium | Rwanda Muslims Association Team | فريق جمعية مسلمي رواندا | org/publisher attribution; lang name best-effort |
| `sd-taj-mehmood-amroti` | `sd-taj-mehmood-amroti.json` | Sindhi | simple | medium | Taj Mehmood Amroti | تاج محمود أمروتي | verify AR translit; lang name best-effort |
| `si-translation-pioneers-center` | `si-translation-pioneers-center.json` | Sinhala | simple | medium | Pioneers Translation Center | — | org/publisher attribution; AR uncertain; generic/work-title name |
| `so-abdullah-hassan-yacoub` | `so-abdullah-hassan-yacoub.fn.json` | Somali | with_footnotes | medium | Abdullah Hassan Yacoub | عبد الله حسن يعقوب | verify AR translit; lang name best-effort |
| `so-mahmud-abduh` | `so-mahmud-abduh.json` | Somali | simple | medium | Mahmud Abduh | محمود عبده | verify AR translit; lang name best-effort |
| `sq-al-ahmeti` | `sq-al-ahmeti.json` | Albanian | simple | medium | Sherif Ahmeti | شريف أحمتي | verify AR translit |
| `sq-al-hasan-efendi` | `sq-al-hasan-efendi.fn.json` | Albanian | with_footnotes | medium | Hasan Efendi Nahi | حسن أفندي ناهي | verify AR translit |
| `sr-dar-al-salam-center` | `sr-dar-al-salam-center.json` | Serbian | simple | medium | Dar Al-Salam Center | مركز دار السلام | org/publisher attribution |
| `sv-knut` | `sv-knut.json` | Swedish | simple | medium | Knut | — | EN from filename; AR unverified; lang name best-effort |
| `sw-ali-muhsin` | `sw-ali-muhsin.json` | Swahili | simple | medium | Ali Muhsin al-Barwani | علي محسن البرواني | verify AR translit; lang name best-effort |
| `sw-dr-abdullah-muhammad-abu-bakr-and-sheikh-nasir-khamis` | `sw-dr-abdullah-muhammad-abu-bakr-and-sheikh-nasir-khamis.json` | Swahili | simple | medium | Dr. Abdullah Muhammad Abu Bakr and Sheikh Nasir Khamis | — | EN from filename; AR unverified; lang name best-effort |
| `sw-swahili-translation-rowad-translation-center` | `sw-swahili-translation-rowad-translation-center.json` | Swahili | simple | medium | Swahili Translation Rowad Translation Center | — | EN from filename; AR unverified; Rowwad spelling variant; generic/work-title name; lang name best-effort |
| `ta-abdul-hameed-baqavi` | `ta-abdul-hameed-baqavi.json` | Tamil | simple | medium | Abdul Hameed Baqavi | — | EN from filename; AR unverified |
| `ta-jan-trust` | `ta-jan-trust.json` | Tamil | simple | medium | Jan Trust | — | EN from filename; AR unverified |
| `ta-sheikh-omar-sharif-bin-abdul-salam` | `ta-sheikh-omar-sharif-bin-abdul-salam.json` | Tamil | simple | medium | Sheikh Omar Sharif bin Abdul Salam | — | EN from filename; AR unverified |
| `te-maulana-abder-rahim-ibn-muhammad` | `te-maulana-abder-rahim-ibn-muhammad.json` | Telugu | simple | medium | Maulana Abder Rahim ibn Muhammad | — | EN from filename; AR unverified |
| `te-muhammad-azeez-ur-rahman` | `te-muhammad-azeez-ur-rahman.json` | Telugu | simple | medium | Muhammad Azeez Ur Rahman | — | EN from filename; AR unverified |
| `tg-ayati` | `tg-ayati.json` | Tajik | simple | medium | Abdulmuhammad Ayati | عبد المحمد آيتي | verify AR translit; lang name best-effort |
| `tg-khawaja-mirof-khawaja-mir` | `tg-khawaja-mirof-khawaja-mir.fn.json` | Tajik | with_footnotes | medium | Khawaja Mirof Khawaja Mir | — | EN from filename; AR unverified; lang name best-effort |
| `tg-pioneers-of-translation-center` | `tg-pioneers-of-translation-center.json` | Tajik | simple | medium | Pioneers of Translation Center | — | org/publisher attribution; AR uncertain; generic/work-title name; lang name best-effort |
| `th-quran-complex` | `th-quran-complex.json` | Thai | simple | medium | Quran Complex | — | EN from filename; AR unverified; generic/work-title name |
| `th-society-of-institutes-and-universities` | `th-society-of-institutes-and-universities.json` | Thai | simple | medium | Society of Institutes and Universities | — | org/publisher attribution; AR uncertain |
| `tl-dar-al-salam-center` | `tl-dar-al-salam-center.json` | Tagalog | simple | medium | Dar Al-Salam Center | مركز دار السلام | org/publisher attribution |
| `tr-dar-al-salam-center` | `tr-dar-al-salam-center.json` | Turkish | simple | medium | Dar Al-Salam Center | مركز دار السلام | org/publisher attribution |
| `tr-diyanet` | `tr-diyanet.json` | Turkish | simple | medium | Turkish Diyanet (Presidency of Religious Affairs) | رئاسة الشؤون الدينية التركية | org/publisher attribution |
| `tr-hamdi` | `tr-hamdi.json` | Turkish | simple | medium | Elmalılı Hamdi Yazır | حمدي يازر | verify AR translit |
| `tr-muslim-shahin` | `tr-muslim-shahin.json` | Turkish | simple | medium | Muslim Shahin | — | EN from filename; AR unverified |
| `tr-shaban-britch` | `tr-shaban-britch.json` | Turkish | simple | medium | Shaban Britch | شعبان بريتش | verify AR translit |
| `ug-saleh` | `ug-saleh.json` | Uyghur | simple | medium | Muhammad Saleh | محمد صالح | verify AR translit |
| `uk-dr-mikhailo-yaqubovic` | `uk-dr-mikhailo-yaqubovic.json` | Ukrainian | simple | medium | Dr. Mykhailo Yakubovych | ميخائيلو ياكوبوفيتش | verify AR translit; lang name best-effort |
| `ur-bayan-ul-quran` | `ur-bayan-ul-quran.json` | Urdu | simple | medium | Bayan ul Quran | — | EN from filename; AR unverified; generic/work-title name |
| `ur-fatah-muhammad-jalandhari` | `ur-fatah-muhammad-jalandhari.json` | Urdu | simple | medium | Fateh Muhammad Jalandhari | فتح محمد جالندهري | verify AR translit |
| `ur-junagarri` | `ur-junagarri.fn.json` | Urdu | with_footnotes | medium | Muhammad Junagarhi | محمد جوناغڑهي | verify AR translit |
| `ur-maududi-roman-urdu` | `ur-maududi-roman-urdu.json` | Urdu | simple | medium | Abul A'la Maududi (Roman Urdu) | أبو الأعلى المودودي | verify AR translit |
| `ur-maulana-wahid-uddin-khan-urdu` | `ur-maulana-wahid-uddin-khan-urdu.json` | Urdu | simple | medium | Maulana Wahiduddin Khan (Urdu) | وحيد الدين خان | verify AR translit |
| `ur-tafsir-e-usmani` | `ur-tafsir-e-usmani.fn.json` | Urdu | with_footnotes | medium | Tafsir e Usmani | — | EN from filename; AR unverified |
| `uz-alauddin-mansour` | `uz-alauddin-mansour.fn.json` | Uzbek | with_footnotes | medium | Alauddin Mansour | علاء الدين منصور | verify AR translit |
| `uz-sodik` | `uz-sodik.fn.json` | Uzbek | with_footnotes | medium | Sodik | — | EN from filename; AR unverified |
| `uz-uzbek-translation-rowwad-translation-center` | `uz-uzbek-translation-rowwad-translation-center.json` | Uzbek | simple | medium | Uzbek Translation Rowwad Translation Center | — | EN from filename; AR unverified; Rowwad spelling variant; generic/work-title name |
| `vi-hasan-abdul-karim` | `vi-hasan-abdul-karim.json` | Vietnamese | simple | medium | Hasan Abdul Karim | حسن عبد الكريم | verify AR translit |
| `vi-translation-pioneers-center` | `vi-translation-pioneers-center.fn.json` | Vietnamese | with_footnotes | medium | Pioneers Translation Center | — | org/publisher attribution; AR uncertain; generic/work-title name |
| `yao-abdul-hamid-silika` | `yao-abdul-hamid-silika.fn.json` | Yao | with_footnotes | medium | Abdul Hamid Silika | — | EN from filename; AR unverified; lang name best-effort |
| `yo-shaykh-abu-rahimah-mikael-aykyuni` | `yo-shaykh-abu-rahimah-mikael-aykyuni.fn.json` | Yoruba | with_footnotes | medium | Shaykh Abu Rahimah Mikael Aykyuni | — | EN from filename; AR unverified; lang name best-effort |
| `zh-chinese-suliman` | `zh-chinese-suliman.json` | Chinese | simple | medium | Chinese Suliman | — | EN from filename; AR unverified |
| `zh-chinese-translation-basair` | `zh-chinese-translation-basair.json` | Chinese | simple | medium | Chinese Translation Basair | — | EN from filename; AR unverified; generic/work-title name |
| `zh-ma-jain` | `zh-ma-jain.json` | Chinese | simple | medium | Ma Jian | محمد ما جيان | verify AR translit |
| `zh-muhammad-makin` | `zh-muhammad-makin.json` | Chinese | simple | medium | Muhammad Ma Jin (Makin) | محمد ماكين | verify AR translit |

## 10. Common normalization decisions (applied)

- **Filename-derived English improved** where unambiguous, e.g. `yusufali` → *Abdullah Yusuf Ali*,
  `bubenheim`/`asad`/`ghali` left as recognizable surnames, initials normalized (`dr-t-b-irving`).
- **`translatorKey` preserved verbatim** from the manifest (technical identity unchanged).
- **Verified Arabic** only for well-known scholars/organizations (e.g. *Saheeh International* →
  صحيح إنترناشونال, *King Fahd Complex* → مجمع الملك فهد لطباعة المصحف الشريف). Everything else is a
  **proposal to verify** or left `null`.
- **`displayNameEn` = `translatorNameEn`** by default; for organization-attributed sources both hold
  the organization name and the record is flagged "individual translator not identified".
- **Rowwad family not silently merged.** `rowad` / `ruwwad` / `rwwad` / `rowwad` spellings are kept as
  found and flagged for a single normalization + entity decision (12 records).
- **Unknown / typo keys** (`*-unknown`, `dv-unknow`, `tt-unknow`, `mrn-unknown`, machine-derived
  `id`) get low confidence and explicit reasons; Arabic left `null`.

## 11. Language names that may need review (best-effort; not from the Feature 007 manifest)

These languages' `languageNameAr` / `nativeName` were filled best-effort during packaging and were
not sourced from the verified Feature 007 tafsir manifest. They are likely correct but warrant a
spot-check.

| code | nameEn | nameAr | nativeName |
|---|---|---|---|
| `ak` | Akan (Asante Twi) | الأشانتية | Akan |
| `am` | Amharic | الأمهرية | አማርኛ |
| `ber` | Amazigh (Berber) | الأمازيغية | Tamaziɣt |
| `bg` | Bulgarian | البلغارية | Български |
| `bm` | Bambara | البامبارية | Bamanankan |
| `ceb` | Cebuano (Bisaya) | البيسايا | Cebuano |
| `cs` | Czech | التشيكية | Čeština |
| `de` | German | الألمانية | Deutsch |
| `dv` | Divehi | المهرية (الديفيهي) | ދިވެހި |
| `el` | Greek | اليونانية | Ελληνικά |
| `fi` | Finnish | الفنلندية | Suomi |
| `fil` | Filipino | الفلبينية | Filipino |
| `gu` | Gujarati | الغوجاراتية | ગુજરાતી |
| `ha` | Hausa | الهوسا | Hausa |
| `hr` | Croatian | الكرواتية | Hrvatski |
| `kk` | Kazakh | الكازاخية | Қазақша |
| `kn` | Kannada | الكانادية | ಕನ್ನಡ |
| `ko` | Korean | الكورية | 한국어 |
| `ks` | Kashmiri | الكشميرية | کٲشُر |
| `ku` | Kurdish | الكردية | Kurdî |
| `ln` | Lingala | اللينغالا | Lingála |
| `lt` | Lithuanian | الليتوانية | Lietuvių |
| `luy` | Luhya | اللوهيا | Luluhya |
| `mdh` | Maguindanao | الماغينداناو | Maguindanaon |
| `mg` | Malagasy | المالاغاشية | Malagasy |
| `mk` | Macedonian | المقدونية | Македонски |
| `mos` | Mossi (Mooré) | الموسي | Mòoré |
| `mr` | Marathi | الماراثية | मराठी |
| `mrw` | Maranao | المارناو | Mëranaw |
| `ms` | Malay | الماليزية | Bahasa Melayu |
| `mt` | Maltese | المالطية | Malti |
| `ne` | Nepali | النيبالية | नेपाली |
| `nl` | Dutch | الهولندية | Nederlands |
| `no` | Norwegian | النرويجية | Norsk |
| `ny` | Chichewa (Nyanja) | الشيشيوا | Chichewa |
| `om` | Oromo | الأورومو | Afaan Oromoo |
| `pa` | Punjabi | البنجابية | ਪੰਜਾਬੀ |
| `pl` | Polish | البولندية | Polski |
| `prs` | Dari | الدرية | دری |
| `pt` | Portuguese | البرتغالية | Português |
| `rn` | Kirundi | الكيروندي | Ikirundi |
| `ro` | Romanian | الرومانية | Română |
| `rw` | Kinyarwanda | الكينيارواندا | Kinyarwanda |
| `sd` | Sindhi | السندية | سنڌي |
| `so` | Somali | الصومالية | Soomaali |
| `sv` | Swedish | السويدية | Svenska |
| `sw` | Swahili | السواحيلية | Kiswahili |
| `tg` | Tajik | الطاجيكية | Тоҷикӣ |
| `tt` | Tatar | التتارية | Татарча |
| `uk` | Ukrainian | الأوكرانية | Українська |
| `yao` | Yao | الياو | Chiyao |
| `yo` | Yoruba | اليوروبا | Yorùbá |

## 12. Final recommendation

**`READY_FOR_SPEC_KIT_WITH_METADATA_REVIEW`.**

This overlay is review-ready for Mohamed. After review, approved display metadata can either be merged
into `manifest.json` or kept as a separate `source-display-metadata.json` importer contract (the
`.review.json` suffix marks this file as not-yet-approved). The manifest's file/hash/source guarantees
are unchanged, so the existing package validation still holds.
