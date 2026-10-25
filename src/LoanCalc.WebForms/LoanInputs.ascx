<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="LoanInputs.ascx.cs" Inherits="LoanCalc.WebForms.LoanInputs" %>
<fieldset class="loan">
    <legend><%: Title %></legend>

    <asp:BulletedList ID="Errors" runat="server" CssClass="errors" Visible="false" />

    <div class="field">
        <asp:Label runat="server" AssociatedControlID="Principal" Text="Principal ($)" />
        <asp:TextBox ID="Principal" runat="server" TextMode="Number" min="1" max="100000000" step="any" required="required" />
    </div>
    <div class="field">
        <asp:Label runat="server" AssociatedControlID="Rate" Text="Annual interest rate (%)" />
        <asp:TextBox ID="Rate" runat="server" TextMode="Number" min="0" max="30" step="any" required="required" />
    </div>
    <div class="field">
        <asp:Label runat="server" AssociatedControlID="Term" Text="Term" />
        <%-- Vue island: buttons in place of the select. The select stays as the posted field and the no-JS fallback;
             the element finds it as its own child, so no client ids are involved. --%>
        <term-picker label="Term" invalid="<%: TermInvalid ? "true" : "false" %>">
            <asp:DropDownList ID="Term" runat="server" />
        </term-picker>
    </div>
    <div class="field">
        <asp:Label runat="server" AssociatedControlID="Fees" Text="Closing costs and fees ($)" />
        <asp:TextBox ID="Fees" runat="server" TextMode="Number" min="0" step="any" />
    </div>
    <div class="field">
        <asp:Label runat="server" AssociatedControlID="Extra" Text="Extra monthly principal ($, optional)" />
        <asp:TextBox ID="Extra" runat="server" TextMode="Number" min="0" step="any" />
    </div>
</fieldset>
