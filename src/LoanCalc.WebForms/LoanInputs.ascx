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
        <asp:DropDownList ID="Term" runat="server" />
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
