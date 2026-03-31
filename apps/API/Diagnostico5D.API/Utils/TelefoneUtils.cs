namespace Diagnostico5D.API.Utils;

public static class TelefoneUtils
{
    public static string NormalizarTelefone(string? telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone))
            return string.Empty;

        return new string(telefone.Where(char.IsDigit).ToArray());
    }

    public static string FormatarParaEvolutionApi(string? telefone, string codigoPaisPadrao = "55")
    {
        if (string.IsNullOrWhiteSpace(telefone))
            throw new ArgumentException("Telefone não pode ser vazio", nameof(telefone));

        var numeroLimpo = NormalizarTelefone(telefone);

        if (string.IsNullOrEmpty(numeroLimpo))
            throw new ArgumentException("Telefone inválido: não contém dígitos", nameof(telefone));

        if (numeroLimpo.StartsWith(codigoPaisPadrao))
            return numeroLimpo;

        if (numeroLimpo.StartsWith("0"))
            numeroLimpo = numeroLimpo.Substring(1);

        if (numeroLimpo.Length == 11)
            return $"{codigoPaisPadrao}{numeroLimpo}";

        if (numeroLimpo.Length == 10)
            return $"{codigoPaisPadrao}{numeroLimpo}";

        if (numeroLimpo.Length == 8 || numeroLimpo.Length == 9)
            return $"{codigoPaisPadrao}11{numeroLimpo}";

        return $"{codigoPaisPadrao}{numeroLimpo}";
    }
}
