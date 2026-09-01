using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using eu.foodmission.platform.Components;
using Unity.AppUI.UI;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FMDialogActionTests
    {
        [Test]
        public void Constructor_SetsLabel()
        {
            var action = new FMDialogAction("OK", null, ButtonVariant.Accent);

            Assert.AreEqual("OK", action.Label);
            Assert.IsNull(action.Callback);
            Assert.AreEqual(action.ButtonVariant, ButtonVariant.Accent);
        }

        [Test]
        public void Constructor_WithDefaultIsPrimary_SetsFalse()
        {
            var action = new FMDialogAction("Cancel", null);

            Assert.AreEqual("Cancel", action.Label);
            Assert.IsNull(action.Callback);
            Assert.AreNotEqual(action.ButtonVariant, ButtonVariant.Accent);
        }
    }

    [TestFixture]
    public class FMDialogShowInfoTests
    {
        [Test]
        public void ShowInfo_ThrowsWhenActionsNull()
        {
            Assert.Throws<ArgumentException>(() =>
                FMDialog.ShowInfo(null, "title", "body", null));
        }

        [Test]
        public void ShowInfo_ThrowsWhenActionsEmpty()
        {
            Assert.Throws<ArgumentException>(() =>
                FMDialog.ShowInfo(null, "title", "body", new FMDialogAction[0]));
        }
    }

    [TestFixture]
    public class CategoryEmojisTests
    {
        [Test]
        public void CategoryEmojis_ContainsExpectedKeys()
        {
            var field = typeof(FMSearchOrCategoryField).GetField("CategoryEmojis",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "CategoryEmojis field should exist");

            var dict = field.GetValue(null) as Dictionary<string, string>;
            Assert.IsNotNull(dict);

            Assert.IsTrue(dict.ContainsKey("alcoholic-beverages"));
            Assert.IsTrue(dict.ContainsKey("vegetables"));
            Assert.IsTrue(dict.ContainsKey("potatoes-and-tubers"));

            Assert.AreEqual("🍺", FMSearchOrCategoryField.GetCategoryEmoji("alcoholic-beverages"));
            Assert.AreEqual("🥦", FMSearchOrCategoryField.GetCategoryEmoji("Vegetables"));
            Assert.AreEqual("🥔", FMSearchOrCategoryField.GetCategoryEmoji("potatoes-and-tubers"));
            Assert.AreEqual("🥛", FMSearchOrCategoryField.GetCategoryEmoji("Milk and milk products"));
        }
    }

    [TestFixture]
    public class FMNutriViewTests
    {
        [Test]
        public void FMNutriView_Instantiation_Succeeds()
        {
            var view = new FMNutriView();
            Assert.IsNotNull(view);
            Assert.IsTrue(view.ClassListContains("fm-nutri-view"));
        }

        [Test]
        public void FMNutriView_OnClick_TriggersCallback()
        {
            var view = new FMNutriView();
            bool clicked = false;
            view.OnClick = () => clicked = true;

            // Invoke private OnNutriClicked handler via reflection to test callback dispatch
            var method = typeof(FMNutriView).GetMethod("OnNutriClicked",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "OnNutriClicked method should exist");

            method.Invoke(view, new object[] { null });
            Assert.IsTrue(clicked, "OnClick callback should be invoked on Nutri click");
        }

        [Test]
        public void FMNutriView_HasClickAction_ReflectsAssignment()
        {
            var view = new FMNutriView();
            Assert.IsFalse(view.HasClickAction);

            view.OnClick = () => { };
            Assert.IsTrue(view.HasClickAction);
        }
    }

    [TestFixture]
    public class FMAvatarViewTests
    {
        [Test]
        public void FMAvatarView_Instantiation_Succeeds()
        {
            var view = new FMAvatarView();
            Assert.IsNotNull(view);
            Assert.IsTrue(view.ClassListContains("fm-avatar-view"));
            Assert.AreEqual(AvatarViewMode.Bust, view.Mode);
        }

        [Test]
        public void FMAvatarView_Mode_CanBeUpdated()
        {
            var view = new FMAvatarView();
            view.Mode = AvatarViewMode.FullBody;
            Assert.AreEqual(AvatarViewMode.FullBody, view.Mode);

            view.Mode = AvatarViewMode.Face2D;
            Assert.AreEqual(AvatarViewMode.Face2D, view.Mode);
        }

        [Test]
        public void FMAvatarView_OnClick_TriggersCallback()
        {
            var view = new FMAvatarView();
            bool clicked = false;
            view.OnClick = () => clicked = true;

            // Invoke private OnAvatarClicked handler via reflection to test callback dispatch
            var method = typeof(FMAvatarView).GetMethod("OnAvatarClicked",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "OnAvatarClicked method should exist");

            method.Invoke(view, new object[] { null });
            Assert.IsTrue(clicked, "OnClick callback should be invoked on Avatar click");
        }

        [Test]
        public void FMAvatarView_HasClickAction_ReflectsAssignment()
        {
            var view = new FMAvatarView();
            Assert.IsFalse(view.HasClickAction);

            view.OnClick = () => { };
            Assert.IsTrue(view.HasClickAction);
        }
    }

    [TestFixture]
    public class FMResponseQuizTests
    {
        [Test]
        public void FMResponseQuiz_Instantiation_SucceedsWithDefaults()
        {
            var quizCard = new FMResponseQuiz();
            Assert.IsNotNull(quizCard);
            Assert.IsTrue(quizCard.ClassListContains("fm-quiz-response-card"));
            Assert.IsTrue(quizCard.ClassListContains("box-background"));
            Assert.IsTrue(quizCard.ClassListContains("fm-shadow-wrapper"));
            Assert.IsTrue(quizCard.Clickable);
            Assert.IsTrue(quizCard.IsClickable);
            Assert.IsFalse(quizCard.HasClickAction);
            Assert.AreEqual("", quizCard.ResponseText);
            Assert.IsNull(quizCard.QuizOption);
        }

        [Test]
        public void FMResponseQuiz_OnClick_TriggersCallbacksWhenClickable()
        {
            var quizCard = new FMResponseQuiz();
            var option = new QuizOption { id = "opt1", text = "Opción A", label = "A" };
            quizCard.QuizOption = option;

            bool eventClicked = false;
            bool actionClicked = false;
            QuizOption selectedOption = null;

            quizCard.Clicked += (card) => eventClicked = true;
            quizCard.OnClick = (card) => actionClicked = true;
            quizCard.OnOptionSelected = (opt) => selectedOption = opt.QuizOption;

            Assert.IsTrue(quizCard.HasClickAction);

            var method = typeof(FMResponseQuiz).GetMethod("OnClickEvent",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "OnClickEvent method should exist");

            method.Invoke(quizCard, new object[] { null });

            Assert.IsTrue(eventClicked, "Clicked event should fire");
            Assert.IsTrue(actionClicked, "OnClick callback should fire");
            Assert.AreEqual(option, selectedOption, "OnOptionSelected should pass QuizOption");
        }

        [Test]
        public void FMResponseQuiz_DisabledClickable_DoesNotTriggerCallbacks()
        {
            var quizCard = new FMResponseQuiz();
            var option = new QuizOption { id = "opt1", text = "Opción A", label = "A" };
            quizCard.QuizOption = option;
            quizCard.Clickable = false;

            bool eventClicked = false;
            bool actionClicked = false;
            QuizOption selectedOption = null;

            quizCard.Clicked += (card) => eventClicked = true;
            quizCard.OnClick = (card) => actionClicked = true;
            quizCard.OnOptionSelected = (opt) => selectedOption = opt.QuizOption;

            var method = typeof(FMResponseQuiz).GetMethod("OnClickEvent",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "OnClickEvent method should exist");

            method.Invoke(quizCard, new object[] { null });

            Assert.IsFalse(eventClicked, "Clicked event should NOT fire when Clickable is false");
            Assert.IsFalse(actionClicked, "OnClick callback should NOT fire when Clickable is false");
            Assert.IsNull(selectedOption, "OnOptionSelected should NOT fire when Clickable is false");
        }

        [Test]
        public void FMResponseQuiz_SetQuizOptionNull_DoesNotThrowAndClearsText()
        {
            var quizCard = new FMResponseQuiz();
            quizCard.QuizOption = new QuizOption { text = "Prueba" };
            Assert.AreEqual("Prueba", quizCard.ResponseText);

            quizCard.QuizOption = null;
            Assert.AreEqual("", quizCard.ResponseText);
            Assert.IsNull(quizCard.QuizOption);
        }

        [Test]
        public void FMResponseQuiz_Clear_ResetsTextAndOption()
        {
            var quizCard = new FMResponseQuiz();
            quizCard.QuizOption = new QuizOption { text = "Prueba" };
            quizCard.ClearContent();

            Assert.AreEqual("", quizCard.ResponseText);
            Assert.IsNull(quizCard.QuizOption);
        }
    }

    [TestFixture]
    public class LinkHelperTests
    {
        [Test]
        public void ParseSource_WithAcademicDoiCitation_ExtractsCleanTextAndDoiLink()
        {
            string raw = "Mazac, R., Meinilä, J., Korkalo, L. et al. Incorporation of novel foods in European diets can reduce global warming potential. Nat Food 3, 286–293 (2022). https://doi.org/10.1038/s43016-022-00489-9";
            var result = LinkHelper.ParseSource(raw);

            Assert.IsNotNull(result);
            Assert.AreEqual("Mazac, R., Meinilä, J., Korkalo, L. et al. Incorporation of novel foods in European diets can reduce global warming potential. Nat Food 3, 286–293 (2022).", result.CitationText);
            Assert.AreEqual(1, result.Links.Count);
            Assert.AreEqual("Ver artículo (DOI) ↗", result.Links[0].Title);
            Assert.AreEqual("https://doi.org/10.1038/s43016-022-00489-9", result.Links[0].Url);
        }

        [Test]
        public void ParseSource_WithStandardWebUrl_ExtractsCleanTextAndDomainLink()
        {
            string raw = "Organización Mundial de la Salud (2023). https://www.who.int/news-room/fact-sheets/detail/healthy-diet";
            var result = LinkHelper.ParseSource(raw);

            Assert.IsNotNull(result);
            Assert.AreEqual("Organización Mundial de la Salud (2023).", result.CitationText);
            Assert.AreEqual(1, result.Links.Count);
            Assert.AreEqual("who.int ↗", result.Links[0].Title);
            Assert.AreEqual("https://www.who.int/news-room/fact-sheets/detail/healthy-diet", result.Links[0].Url);
        }

        [Test]
        public void ParseSource_WithMarkdownLink_ExtractsTitleAndUrl()
        {
            string raw = "Estudio publicado en [Nature Food](https://doi.org/10.1038/s43016-022-00489-9) sobre dietas sostenibles.";
            var result = LinkHelper.ParseSource(raw);

            Assert.IsNotNull(result);
            Assert.AreEqual("Estudio publicado en Nature Food sobre dietas sostenibles.", result.CitationText);
            Assert.AreEqual(1, result.Links.Count);
            Assert.AreEqual("Nature Food ↗", result.Links[0].Title);
            Assert.AreEqual("https://doi.org/10.1038/s43016-022-00489-9", result.Links[0].Url);
        }

        [Test]
        public void ParseSource_WithPlainTextOnly_ReturnsCitationWithoutLinks()
        {
            string raw = "Informe de Sostenibilidad Alimentaria 2024.";
            var result = LinkHelper.ParseSource(raw);

            Assert.IsNotNull(result);
            Assert.AreEqual("Informe de Sostenibilidad Alimentaria 2024.", result.CitationText);
            Assert.AreEqual(0, result.Links.Count);
            Assert.IsFalse(result.HasLinks);
            Assert.IsFalse(result.IsEmpty);
        }

        [Test]
        public void ParseSource_WithNullOrWhitespace_ReturnsNull()
        {
            Assert.IsNull(LinkHelper.ParseSource(null));
            Assert.IsNull(LinkHelper.ParseSource(""));
            Assert.IsNull(LinkHelper.ParseSource("   "));
        }

        [Test]
        public void ExtractLinks_WithMultipleUrls_ExtractsAllWithoutDuplicates()
        {
            string raw = "Consulta https://who.int y también https://fao.org/nutrition";
            var links = LinkHelper.ExtractLinks(raw);

            Assert.AreEqual(2, links.Count);
            Assert.AreEqual("https://who.int", links[0].Url);
            Assert.AreEqual("https://fao.org/nutrition", links[1].Url);
        }
    }

    [TestFixture]
    public class FMSourceItemViewTests
    {
        [Test]
        public void FMSourceItemView_InitialState_IsHidden()
        {
            var view = new FMSourceItemView();
            Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.None, view.style.display.value);
        }

        [Test]
        public void FMSourceItemView_SetSourceWithDoi_PopulatesAndShows()
        {
            var view = new FMSourceItemView();
            string raw = "Mazac et al. (2022). https://doi.org/10.1038/s43016-022-00489-9";
            view.SetSource(raw);

            Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.Flex, view.style.display.value);
            Assert.AreEqual("Mazac et al. (2022).", view.CitationText);
            Assert.IsNotNull(view.SourceInfo);
            Assert.AreEqual(1, view.SourceInfo.Links.Count);
        }

        [Test]
        public void FMSourceItemView_SetSourceNull_HidesElement()
        {
            var view = new FMSourceItemView();
            view.SetSource("Test (2022). https://example.com");
            Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.Flex, view.style.display.value);

            view.SetSource((string)null);
            Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.None, view.style.display.value);
            Assert.AreEqual("", view.CitationText);
        }

        [Test]
        public void FMSourceItemView_SetSourceDirectCitationAndUrl_PopulatesCorrectly()
        {
            var view = new FMSourceItemView();
            view.SetSource("FAO Report 2024", "https://fao.org", "FAO Report");

            Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.Flex, view.style.display.value);
            Assert.AreEqual("FAO Report 2024", view.CitationText);
            Assert.AreEqual(1, view.SourceInfo.Links.Count);
            Assert.AreEqual("FAO Report", view.SourceInfo.Links[0].Title);
            Assert.AreEqual("https://fao.org", view.SourceInfo.Links[0].Url);
        }

        [Test]
        public void FMSourceItemView_ShowPrefix_TogglesHeader()
        {
            var view = new FMSourceItemView();
            Assert.IsTrue(view.ShowPrefix);

            view.ShowPrefix = false;
            Assert.IsFalse(view.ShowPrefix);

            view.ShowPrefix = true;
            Assert.IsTrue(view.ShowPrefix);
        }
    }
}
