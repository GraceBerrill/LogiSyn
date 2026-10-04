using SharedLibrary.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLibrary.Interface
{
    public interface ITempRecipeService
    {
        ProductRecipeModel? FindRecipeByProductName(string productName);
        IEnumerable<ProductRecipeModel> GetAllRecipes();
    }
}
