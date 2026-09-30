using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Units from GET /api/v1/catalog/units, with the base codes as fallback so lists are never null.
    /// </summary>
    public interface IUnitCatalog
    {
        /// <summary>Unit codes, same order as <see cref="Labels"/>.</summary>
        IReadOnlyList<string> Codes { get; }

        /// <summary>Localized labels, same order as <see cref="Codes"/>.</summary>
        IReadOnlyList<string> Labels { get; }

        /// <summary>Localized label for a code; the code itself when unknown; "" for null/empty.</summary>
        string GetLabel(string code);

        /// <summary>Unit code for free text (code, localized label or common alias); null when unrecognised.</summary>
        string ResolveCode(string raw);

        /// <summary>Parses "200 g", "1,5 kg", "2". False when there is no leading number (outputs 1 and the default unit).</summary>
        bool TryParseMeasure(string measure, out float quantity, out string unit);

        /// <summary>Loads labels for a language. Idempotent per language; failures keep the current lists.</summary>
        Task LoadAsync(string lang);
    }
}
