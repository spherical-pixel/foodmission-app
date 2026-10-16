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
