v0.1.0 - Changelog
First version for the pilot participants. The in-app notes summarise what the platform offers; the technical changes since 0.0.12 are below.

✨ New Features & Modules
- Environment: builds point to Production by default (active `ApiEnvironmentConfig` entry and `ApiConfig` fallback).
- Searchable picker (`FMSearchablePicker`) for country, region, year of birth, cuisine and recipe category (#94).

🐛 Bug Fixes
- Edit Profile: the saved region is kept when the region list reloads, and no longer overwritten on save (#90).
- Forms: long field questions wrap instead of being truncated (#92).
- Dialogs: full-screen info dialogs (What's New) respect the safe area.
- Login: the screen scrolls on small screens.

🔒 Release hardening
- Release builds filter `Debug.Log` to warnings and errors; logs with request/response bodies, tokens or user data only in `UNITY_EDITOR || DEVELOPMENT_BUILD`. `ApiErrorHelper` keeps code/message/traceId, full error body only in development builds.
- iOS: `aps-environment` follows the development build flag. Insecure HTTP allowed only in development builds.
- Android: not flagged as a game; notifications open `AppUIGameActivity`.

v0.0.12 - Changelog
✨ New Features & Modules
- Progress wheels: tapping a wheel card on Home opens an explanation screen with the partner text, the 20 scenarios with this wheel's values (local copy of the backend impact coefficients) and the scientific sources; 9 languages.
- Check-in: day-based missions (M.I1.3, M.B5.5, M.B6.1/2/4, M.A6.3, M.I6.3) and swap missions (M.B1.4, M.B1.5, M.B3.5, M.B6.3, M.B6.5, M.I1.1, M.I1.2, M.I3.3, M.I6.1) are asked once per day on the day card and sent as OTHER meal logs; water/snack swaps are counters; answered days are remembered per user and mission attempt; after sending, Nutri lists each mission's progress. Missions at 100% waiting for their window end get no stalled/last-day nudge.
- Food facts: the read list comes from `GET /food-facts/me` (local list kept as fallback).

🐛 Bug Fixes
- Register: username validated locally; Keycloak/validation errors (invalid or duplicate username, duplicate email, password policy, invalid email) mapped to localized messages on their step.
- Challenges: the product challenge session ends when leaving the helper module flow; the card explains it belongs to a challenge in progress, counts with "Count this product" and can be dismissed with "Not now" (#84).
- Step flow: steps clip their content and check-in chips wrap long answers.
- Learning: quiz and food fact cards scroll when the text does not fit.
- Swaps ending in meat uncheck meat-free/vegan, and such a meal is never sent as MEAL_MEAT_FREE.
- Onboarding: "gender" instead of "genre" in the gender placeholder (en).

🎨 UI / UX & Enhancements
- Typography: default text size one step larger (old medium → small, old large → medium, new large = old large ×1.15); stored scale values unchanged (#82).
- Food facts: "Did you know...?" heading and "Got it!" button; mission and challenge detail cards start with a MISSION / CHALLENGE heading (#79).
- My Goals: explanation for each dimension and topic, starts unselected; the content filter is switched off (`GoalContentFilter.Enabled`) (#86).
- Localization: updated translations from the linguists' review.

v0.0.11 - Changelog
✨ New Features & Modules
- Meal log: day picker (today and the previous 6 days) in the full and the quick log; past days are stamped at a typical local time for the meal type (sent in UTC), edits keep their original timestamp (#65).
- Rewards: the celebration shows the name of the completed quest, mission, challenge, quiz or food fact under the title; larger title and points that fit inside the coin (#66).

🐛 Bug Fixes
- Food search: adapted to the OFF `hasMore` pagination (no total count). With both sources on, results come in chunks of 5 and the other source fills the page when one runs out; "Page N" when the total is unknown; no error dialog when the other source has results. Result rows grow with long names and large fonts.
- Text fields: the caret of an empty field is drawn where the text will appear (`EmptyFieldCaret`, works around a UI Toolkit bug in Unity 6.3–6.7) (#69).
- Catalog: the bundled countries/regions JSON fallback is gone; lists come only from the catalog endpoints.

🎨 UI / UX & Enhancements
- Typography: monotonic font scale per text size (small/medium/large) and role-based text sizes; hardcoded px replaced with scale variables.
- Bottom navigation: fixed label size (body-sm) that wraps to two lines instead of autosizing.
- Shopping list: larger rounded checkboxes with a wider touch target.

v0.0.10 - Changelog
✨ New Features & Modules
- Section tutorials: Nutri explains each main section on the first visit (Home is a 5-step sequence ending on the profile drawer); a ? button in the AppBar replays it. Seen tutorials are synced in `settings.seenTutorials`. "Daily challenge" renamed to "Challenges".
- Onboarding: progress screen between steps and a visible Skip; skipping defers the Home quest reminder for the session.
- Quests: mission and challenge rows open their detail with a "Go to quest" link when outside the current quest; the quest detail redirects to Home when the active quest is completed on the server.
- Profile: "None of the above" for dietary preferences (saves `[]`) and "Prefer not to say" for annual income (saves `null`), with clearer labels; long checkbox texts wrap.
- Recipes: multiple tags per recipe in the editor; before adding ingredients to the shopping list the app checks the pantry (selection dialog with quantity difference); "For you" tells an empty pantry apart from no matching recipes.
- Meal log: switch between full and simplified log, quick log tutorial, clearer Log/Continue buttons and wrapping item rows.

🐛 Bug Fixes
- Food search: shared paged search (`FoodSearchFlow`), API order kept, stale pagination hidden; typed text kept when tapping outside the field.
- Barcode: clear scan errors, retry, and lookup of 12-digit codes.
- iOS keyboard: ignore touches replayed on keyboard show/hide and keep focus when the replayed touch lands below the moved panel.
- Rewards: the celebration shows the user's real XP and level.
- Levels: explanation of the per-area level, suggestion only on changed rows, readable "show previous levels" row in light mode.
- Badges: not-earned badges faded.
- Food groups in backend order, alcoholic beverages last.
- Pantry: cached pantry id dropped when the logged-in user changes.
- Recipes: ingredient measures shown in full in the recipe detail.
- Localization: distinct quest, mission and challenge terms in every language; On/Off in the notifications stepper; unused keys removed.
- Auth: password eye icon visible in light mode.

v0.0.9 - Changelog
🐛 Bug Fixes
- Light mode: readable text and icons on every screen. Boxes with a light background keep their own text colors (`--fm-box-text`), icon-only buttons over the green background use `fm-icon-button--on-main-bg`, and texts follow one rule (dark on white cards, white on the green background, mint on translucent boxes). Covers food info, food waste overlay, group detail, recipes, meal log swaps, notifications, mission check-in and food comparison.
- Navigation: Back returns to the previous screen after the auto-opened shopping list and after saving a recipe (single `Navigate` with `popUpTo` instead of `PopBackStack()` + `Navigate()`).
- Recipes: ingredient units stored as catalog codes with an invariant number ("1.5 CUPS") and shown localized; unrecognized measures stay as written.
- Onboarding survey questions requested with `?lang=` (the backend ignores `Accept-Language`).
- AppBar titles localized, missing UI keys added, App UI 2.1 theme variables used instead of non-existent ones.
- `AppState.Copy()` deep-copies every nested model.

🛠️ Dev & Build
- EditMode tests on GitHub Actions with GameCI (required check on `main`), plus static checks for UXML bindings, localization keys, navigation assets, theme variables, DI registrations and reducers.

v0.0.8 - Changelog
✨ New Features & Modules
- Quests: Quest catalog (`QuestsScreen`) and detail with an activity timeline (`QuestDetailScreen`), active quest from `GET current-quest` (`FMActiveQuestCard` on Home), sequential and level-based unlocking, confirmation when switching quests, onboarding survey required before starting, and celebrations when a quest or a whole dimension is completed.
- Missions: Each mission is started explicitly (`POST …/progress/start`, backend #424) with inputs for M.B1.3 (`previousMeatMeals`), failed missions and restart, start/end date and current day, last-day reminder, and progress percent in the quest timeline.
- Check-in with Nutri (`MissionCheckInScreen`): Guided mission reporting driven by an interaction catalog (replaces `ActivityEventMapper`), past days via `createdAt` (never before `startedAt`), grouped meal logs, idempotent sending with retries and pacing to avoid 429s; questions explain each mission's goal.
- Challenges: Challenges list, detail with in-app completion (`ChallengeCompletionService`) and protein footprint comparison (`FoodComparisonScreen`, CH.B1.1). Challenge and product mission cards in `FoodInfoOverlay` to report products explicitly.
- Knowledge: Knowledge Hub with Quizzes (`QuizScreen`, random quiz, background music) and "Did you know...?" facts (`FoodFactService`, read filter, backend titles), shared through the native share sheet.
- Gamification: `GamificationService`, wallet/XP in Redux, `RewardCelebrationDialog` with a sequential queue of server-detected rewards, badges (`BadgeService`, `BadgesScreen` in the drawer) and per-dimension level-ups (`DIMENSION_LEVEL_UP`).
- Goals & Levels: Goals screen, backend-owned per-dimension levels (screen after the survey and in Edit Profile), content locked above the user's level and hidden below it, filtering by goals.
- Home: Progress wheels ("Your impact") with customization, knowledge bars from the backend, prioritized prompt queue and Nutri reminders (choose a quest, start missions, check in, next "Did you know...?").
- Surveys: Backend-driven onboarding survey (`GET onboarding-survey`) and pilot surveys (`PilotSurveyScreen`, `FMLikertSlider`) in strict order, never two on the same day.
- Notifications: Hybrid push (Firebase Messaging) and local notifications, preferred time in onboarding and Settings, pantry expiry reminders and localized texts.
- Personalization: Nutri editor with accessories, and appearance settings (language, text size, font, theme) from Login and Register.
- Drawer & Profile: Contact support (feedback@foodmission.eu, prefilled email), share the app, read legal documents and delete account from Edit Profile.

🍽️ Meal Log, Pantry & Recipes
- Quick meal log: New flags (nutrition & health, food waste), swap-only logs, mission filter, review step with backend draft meal logs, and flags/swaps shown on the cards.
- Meal log: Day selector stepper and card deletion (editing stays disabled for now).
- Pantry: Deletion with a food waste modal and expired items sync; expiry dates are computed by the backend.
- Food waste: Redesigned screen with pantry search, expired items and monthly history.
- Recipes: Star ratings, cuisine and origin filters, TheMealDB categories, reworked ingredients editor, and edit/delete for the owner only.
- Units: `IUnitCatalog` service with localized labels.

🎨 UI / UX & Enhancements
- `FMLoadingOverlay` on every screen that loads or saves, with owner-aware hiding on exit.
- Barcode scanner as a full-screen App UI modal.
- Music system, new icons and avatar animations.
- DejaVu Sans as global fallback font; reviewed light/dark palette and styles.
- Lists: scroll with the navigation ScrollView and a bottom spacer so the last item clears the nav bar.
- Localization: Hardcoded texts moved to the tables, no hardcoded fallbacks, mascot name via `{global.PET_NAME}`, unused keys removed, and Excel export/import for partner review. Remote localization overrides removed.
- Release notes: One file per version in `version-check/release-notes/`.

🐛 Bug Fixes & Refactor
- Session: Kept when the backend is unreachable; logout clears each user's data and detaches the push device; pure logout reducer.
- API: Every request with a timeout and a fresh token (`ApiRequest.SendAsync`).
- Local cache kept per user; culture-safe storage and expiry dates.
- 429s on Home: shared quest requests and brief reuse of the progress list.
- Style, localization and test fixes (EditMode suite green).

🛠️ Dev & Build
- Dev tools: meal log date offset, advancing days on the local backend and the device together, `FM_UNLOCK_ALL_QUESTS`.
- Android: Gradle templates, Firebase Messaging and EDM4U.

v0.0.7 - Changelog
✨ New Features & Modules
- Recipes Module: Recipe catalog (`RecipeBookScreen`), detail view (`RecipeDetailScreen`) and step-by-step progressive editor (`RecipeEditorScreen`). Ingredients can be sent to the shopping list and recipes logged as meals.
- Quick Search (`QuickSearchScreen`): Unified global food search screen (products, generic foods and barcode scanner).
- Nutrition Viewer (`FoodInfoOverlay`): Full nutrition information view for products and generic foods (NutriScore, NOVA, EcoScore, allergens, ingredients and macro traffic lights).
- Group Management (`GroupDetailScreen`): Detailed view of group members (`FMItemMember`), `ADMIN`/`VIRTUAL` role assignment, group editing and member management.
- Extended Profile & Onboarding: `OnboardingProfileScreen` refactored into 7 steps with the Nutri assistant, a fully dynamic questionnaire based on the backend catalog, and full editing of user segment/motivations in `EditProfileScreen`.
- Avatar Capture: Automatic 2D profile picture generated from the 3D avatar (`AvatarService`) for the app headers.
- Event Logging (`EventService`): User session telemetry (`APP_SESSION_OPENED`, `APP_SESSION_ENDED`, foreground duration).

🔊 Audio & Haptics
- Sound System (`AudioService`): Volume control for sound effects (SFX) and music (BGM) integrated with Unity's AudioMixer.
- Haptic Feedback & Global Clicks: Automatic sounds when tapping buttons (`FMButton`, `Button`, etc.) and haptic (vibration) feedback.
- Nutri Voices: Dynamic audio clips for the Nutri mascot during dialogs and screen transitions.

🎨 UI / UX & Enhancements
- Pantry & Meal History: Automatic pantry deduction by expiry date when logging meals, and dates shown on meal presets with similar names.
- Optimized Barcode Scanner: Torch support on iOS (`IOSTorchPlugin`) and Android (`AndroidTorchPlugin`), ROI restricted to 70%x45% and duplicate-frame filtering to remove false positives.
- Markdown Dialogs: `BrewedInk.MarkdownSupport` package integrated for rich formatting in legal and informational dialogs.
- Unavailable Menu Indicators: Informational notices in sections still under development.
- Complete Logout: Full cleanup of Redux state, local cache, credentials and temporary avatar files on logout.

🐛 Bug Fixes & Refactor
- Registration Form: Pilot consent forms loaded dynamically by country.
- Logout & Token Refresh: Clean redirect after session expiry and automatic token refresh on 401 errors.
- Fixed USS selectors (`:first-child`/`:last-child`) with explicit C# classes in `FoodInfo`.
- Profile DTO Normalization: User segment moved to the request root (`ProfileUpdateRequest`) to match the database.

v0.0.6 - Changelog
✨ New Features
- Meal Log redesigned: 3-step progressive flow (type → source → compose) with separate meal name field and dedicated "Load preset" popover
- Smart save logic: snapshot diff detects modifications — reuses existing meal when unchanged, updates when modified with same name, creates new meal when name changed
- Recipe ingredient dedup: duplicate generic foods in recipe ingredients are now merged (quantities combined)
- FMButton component: new reusable button built on AppUI.UI.Button with shadow support
- Settings screens: un-commented and wired up settings/preferences now that API supports them

🐛 Bug Fixes
- Recipe detection: added isRecipe discriminator to Meal model — recipeId alone is unreliable since meals created from recipes also have it
- Search result labels: FMSearchOrCategoryField now correctly shows GenericFood.foodName instead of C# type name in confirmation panel
- Async void safety: OnPresetSearchChanged wrapped in try-catch to prevent crashes on API failure
- ShoppingListDetail: _progressLabel now properly assigned in CacheUIElements
- Safe area: removed duplicate safearea check from MealLogScreen, restored in PantryScreen.uxml

♻️ Refactors
- FMQuantityUnitPanel: extracted from 5 duplicate qty/unit implementations into reusable component
- Preset search results: now use global fm-scf-result-row class for visual consistency with search results

🧪 Tests
- 8 new tests covering HasModifications, SaveAsync branches, and ConfirmUpdateAndSave

v0.0.5 — Changelog
🆕 Added
- FMSearchOrCategoryField — unified search, category browsing, and barcode scan button for Pantry and ShoppingListDetail screens, replacing FMProductSearchDialog

🎨 Updated
- Avatar editor — cleaned up UXML (removed AppUI theme classes from root, fixed stylesheet references, removed placeholder text)

🔧 Infrastructure
- Model naming migration — food → foodProduct, foodCategory → genericFood to align with API v1
- Localization updates — changed preliminary machine translations to only English by now

v0.0.4 — Changelog
🆕 Added
- Partial screen reader support — AccessibilityService + IAccessibleComponent for 26 screens
- App update check — AppUpdateService fetches version from GitHub, shows ForceUpdateScreen with forced/optional flows
- API environment switching — ApiEnvironmentConfig ScriptableObject to toggle between Staging/Test/Local at runtime
- FMItemListShoppingList — reusable shopping list item component
- FMSearchOrCreateField — reusable search-or-create input component

🎨 Updated
- ShoppingListScreen redesign with new item component and improved layout
- AlertDialog styles and panel adjustments
- Form field components (arrow steppers, checkbox, dropdown, int field, text field) now implement IAccessibleComponent
- Navigation graph updates and cleanup
- FMDialog, LoginScreen, RegisterScreen, SettingsScreen, splash flow, onboarding screens, profile screens, food waste screens, groups screens — accessibility integration and layout fixes
🐛 Fixed

- Arrow stepper flex layout — FMArrowStepper and FormFieldItemArrowStepperSettings now properly shrink and grow
- Avatar editor: Save/Exit button visibility per entry point, AppBar behavior
- FMDialog button layout and icon spacing
- FormFieldItemBase label and input alignment
- Shopping list view model null filter fix
- Disabled Unity Connect service
🔧 Infrastructure

- Updated Foodmission.slnx
- FoodmissionApp.cs and FoodmissionAppBuilder.cs — service registration for Accessibility and AppUpdate
- latest-version.json — version manifest for app update feature

v0.0.3 — Changelog
🆕 Added
- Barcode scanner — preliminary support with camera preview and ZXing decoding (BarcodeScanOverlay)
- Nutri camera — functions to enable/disable cameras in Avatar and Nutri (interactive mascot)
- iOS & Android metadata for localizations (App Store / Play Store descriptions, keywords, etc.)

🐛 Fixed
- Avatar Editor: error preventing all customization options from showing; fixed eyebrows ordering
- Avatar materials: now using MaterialPropertyBlock to avoid dirtying assets on disk
- Android system bars: transparency and re-application after notification shade dismiss
- KeyboardPanelAdjuster: general virtual keyboard panel adjustment
- KeyboardService on Android: proper fallback when TouchScreenKeyboard.area.height returns 0
- Localization: minor preload issue

🎨 Optimized
- RenderTextures: reduced sizes for performance (Avatar + Nutri)

🔧 Other
- Added missing localization assets
- Store upload preparation (version 0.0.3)

v0.0.2 — Changelog
🆕 Added
- Authentication & session: login, register, forgot password, token refresh with proactive timer, Keycloak logout
- Home dashboard: calorie circular progress, status bars, day/meal steppers
- Meal Logging: full CRUD with in-memory cache, meal search, create on-the-fly
- Pantry: items CRUD, food import from barcode, food category service with cache
- Shopping Lists: full CRUD for lists and items, toggle checked, clear checked, barcode import
- Groups: CRUD, join via invite code, member management (add/remove/make-admin)
- Food Waste: logging, statistics, trends
- Avatar Editor: 3D character customization (hair, eyes, nose, mouth, skin, clothes), onboarding integration
- Profile screen: avatar header, stats, action buttons
- Settings screen: font selector, theme (light/dark/system), scale, language
- Notifications: bottom sheet panel with notification cards, overflow menu (view/delete)
- Nutrition mascot (Nutri): 3D model, animations, render texture, dialog screens
- FMDialog: reusable dialog component (alert, confirm, scrollable, custom)
- Form components: text fields, password, dropdown, checkbox, stepper, arrow stepper
- Components: FMStatusBar, AvatarController, FMProductSearchDialog, BarcodeScanOverlay
- Services: TemplateService (UXML from addressables), CatalogService (startup reference data), KeyboardService, NutriService

🔧 Infrastructure
- Unity 6 upgrade (6000.3.12f1), URP, AppUI v2.1.6, Input System
- MVVM + Redux architecture with centralized AppState
- Navigation graph with declarative routing for all screens
- Addressables for UXML/USS asset management
- Localization system connected to Google Sheets, AppUI tables
- TemplateService for loading UXML from addressables with preloading during splash
- Newtonsoft.Json 3.2.1 for OpenFoodFacts deserialization
- All screens migrated from inline loading to TemplateService

🐛 Fixed
- Navigation stopping after login
- Duplicated NutriController on hot reload
- Nutri RenderTexture not working on Android/iOS
- Bottom nav bar label overflow
- Stepers styling
- Token refresh flow
- Missing localizations and preload issues
- Dialog/Modal API usage in FMDialog
- AppUI namespace ambiguities (TextField, Button, Q<>)
- Checkbox value type (bool → CheckboxState)
- FloatField placeholder vs label
- Profile drawer integration with nav framework

🎨 Styling & Theme
- Complete light/dark themes with green-based palette
- Font system: OpenSans, Roboto, OpenDyslexic (Regular/Bold SDF)
- Global keyboard avoidance for mobile (iOS/Android)
- Safe area handling via code
- All USS/TSS organized and cleaned
- Custom arrow icons and app icons

🧪 Testing
- Model serialization tests (Food, ShoppingList, Group, Pantry, Meal, etc.)
- Service tests for Auth, Profile, etc.
- FMDialogAction tests
- Shopping list test fixes
