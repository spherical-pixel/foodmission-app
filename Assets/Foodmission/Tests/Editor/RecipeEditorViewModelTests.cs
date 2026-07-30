using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using eu.foodmission.platform;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class RecipeEditorViewModelTests
    {
        private TestStoreService _storeService;
        private Mock<IRecipeService> _mockRecipeService;
        private RecipeEditorViewModel _viewModel;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _mockRecipeService = new Mock<IRecipeService>();
            _viewModel = new RecipeEditorViewModel(_storeService, _mockRecipeService.Object);
            FoodProductFlow.UseDirectClientOverride = () => false;
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = null;
        }

        [Test]
        public void ValidateStep_Step0_WithEmptyTitle_ReturnsFalse()
        {
            _viewModel.Title = "";
            Assert.IsFalse(_viewModel.TestValidateStep(0));
            Assert.IsNotNull(_viewModel.ErrorDetail);
        }

        [Test]
        public void ValidateStep_Step0_WithTitle_ReturnsTrue()
        {
            _viewModel.Title = "Pasta";
            Assert.IsTrue(_viewModel.TestValidateStep(0));
            Assert.IsNull(_viewModel.ErrorDetail);
        }

        [Test]
        public void ValidateStep_Step1_AlwaysReturnsTrue_SetsWarningIfEmpty()
        {
            _viewModel.Ingredients.Clear();
            Assert.IsTrue(_viewModel.TestValidateStep(1));
            Assert.IsTrue(_viewModel.HasNoIngredientsWarning);
        }

        [Test]
        public void AddFreeTextIngredient_AddsToIngredientsList()
        {
            _viewModel.AddFreeTextIngredient("Salt", "1 tsp");
            Assert.AreEqual(1, _viewModel.Ingredients.Count);
            Assert.AreEqual("Salt", _viewModel.Ingredients[0].Name);
            Assert.AreEqual("1 tsp", _viewModel.Ingredients[0].Measure);
            Assert.IsTrue(_viewModel.Ingredients[0].IsFreeText);
        }

        [Test]
        public void AddIngredientFromGenericFood_WithInvalidUuid_SetsError()
        {
            _viewModel.AddIngredientFromGenericFood("not-a-uuid", "Salt", "1 tsp");
            Assert.AreEqual(0, _viewModel.Ingredients.Count);
            Assert.IsNotNull(_viewModel.ErrorDetail);
        }

        [Test]
        public void AddIngredientFromGenericFood_WithValidUuid_AddsToList()
        {
            _viewModel.AddIngredientFromGenericFood("550e8400-e29b-41d4-a716-446655440000", "Salt", "1 tsp");
            Assert.AreEqual(1, _viewModel.Ingredients.Count);
        }

        [Test]
        public void RemoveIngredient_RemovesAtCorrectIndex()
        {
            _viewModel.AddFreeTextIngredient("Salt", "1 tsp");
            _viewModel.AddFreeTextIngredient("Pepper", "2 tsp");
            _viewModel.RemoveIngredient(0);
            Assert.AreEqual(1, _viewModel.Ingredients.Count);
            Assert.AreEqual("Pepper", _viewModel.Ingredients[0].Name);
        }

        [Test]
        public async Task SaveAsync_OnCreate_CallsCreateRecipeAsyncAndDispatchesNav()
        {
            string navAction = null;
            _viewModel.NavigationRequested += (action, args) => navAction = action;
            _viewModel.Title = "Pasta";
            _mockRecipeService.Setup(s => s.CreateRecipeAsync(It.IsAny<CreateRecipeRequest>()))
                .ReturnsAsync((new Recipe { id = "new-1", title = "Pasta" }, null));

            await _viewModel.SaveAsync();

            _mockRecipeService.Verify(s => s.CreateRecipeAsync(It.IsAny<CreateRecipeRequest>()), Times.Once);
            Assert.IsFalse(_viewModel.IsEditMode);
            Assert.AreEqual("recipes_to_detail", navAction);
        }

        [Test]
        public async Task SaveAsync_OnEdit_CallsUpdateRecipeAsyncWithId()
        {
            _viewModel.Title = "Updated Pasta";
            var field = typeof(RecipeEditorViewModel).GetField("m_EditingRecipeId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_viewModel, "recipe-1");

            _mockRecipeService.Setup(s => s.UpdateRecipeAsync("recipe-1", It.IsAny<CreateRecipeRequest>()))
                .ReturnsAsync((new Recipe { id = "recipe-1", title = "Updated Pasta" }, null));

            await _viewModel.SaveAsync();

            _mockRecipeService.Verify(s => s.UpdateRecipeAsync("recipe-1", It.IsAny<CreateRecipeRequest>()), Times.Once);
            Assert.IsTrue(_viewModel.IsEditMode);
        }

        [Test]
        public async Task SaveAsync_OnError_SetsErrorDetailAndDoesNotNavigate()
        {
            _viewModel.Title = "Pasta";
            _mockRecipeService.Setup(s => s.CreateRecipeAsync(It.IsAny<CreateRecipeRequest>()))
                .ReturnsAsync((null, new ApiErrorResponse { message = "save failed" }));

            await _viewModel.SaveAsync();

            Assert.IsNotNull(_viewModel.ErrorDetail);
            CollectionAssert.DoesNotContain(_storeService.DispatchedActionTypes, "recipes_to_detail");
        }
    }
}
