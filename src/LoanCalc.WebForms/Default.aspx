<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="LoanCalc.WebForms._Default" %>
<%@ Register Src="~/LoanInputs.ascx" TagPrefix="lc" TagName="LoanInputs" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Compare two loans</title>
    <link href="Content/site.css" rel="stylesheet" />
</head>
<body>
    <form id="form1" runat="server">
        <main class="page">
            <header class="page-header">
                <h1>Compare two loans</h1>
                <p class="muted">Web Forms version: the server calculates on each postback.</p>
            </header>

            <div class="loans">
                <lc:LoanInputs ID="LoanA" runat="server" Title="Loan A" />
                <lc:LoanInputs ID="LoanB" runat="server" Title="Loan B" />
            </div>

            <asp:Button ID="CompareButton" runat="server" Text="Compare" CssClass="button" OnClick="CompareButton_Click" />

            <asp:Panel ID="ResultsPanel" runat="server" Visible="false" CssClass="results">
                <table>
                    <caption>Results <span class="muted">(the lower value in each row is highlighted)</span></caption>
                    <thead>
                        <tr><th scope="col"></th><th scope="col">Loan A</th><th scope="col">Loan B</th></tr>
                    </thead>
                    <tbody>
                        <asp:Repeater ID="ResultRows" runat="server" ItemType="LoanCalc.WebForms.ResultRow">
                            <ItemTemplate>
                                <tr>
                                    <th scope="row"><%#: Item.Label %></th>
                                    <td class="<%#: Item.ACssClass %>"><%#: Item.A %></td>
                                    <td class="<%#: Item.BCssClass %>"><%#: Item.B %></td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </tbody>
                </table>
                <p class="muted note">APR includes fees and is based on the scheduled payments only. Extra payments
                    lower total interest and total payback but don't change APR.</p>
            </asp:Panel>
        </main>
    </form>
</body>
</html>
