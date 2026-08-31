namespace MyDmsVn.Bootstrap5WinFormUI.Formatting;

/// <summary>Identifies credit-card formatting metadata inferred from an IIN prefix.</summary>
public enum BootstrapCreditCardType
{
    /// <summary>Unknown or general card layout.</summary>
    General,
    /// <summary>UATP.</summary>
    Uatp,
    /// <summary>American Express.</summary>
    AmericanExpress,
    /// <summary>Diners Club.</summary>
    Diners,
    /// <summary>Discover.</summary>
    Discover,
    /// <summary>Mastercard.</summary>
    Mastercard,
    /// <summary>Dankort.</summary>
    Dankort,
    /// <summary>Instapayment.</summary>
    Instapayment,
    /// <summary>Fifteen-digit JCB.</summary>
    Jcb15,
    /// <summary>JCB.</summary>
    Jcb,
    /// <summary>Maestro.</summary>
    Maestro,
    /// <summary>Visa.</summary>
    Visa,
    /// <summary>MIR.</summary>
    Mir,
    /// <summary>UnionPay.</summary>
    UnionPay
}
