<%@ Page Language="C#" %>
<%--
    Host page for the Vue <loan-compare> custom element. No code-behind and no <form runat="server">:
    the element owns its form (inside its shadow root) and talks to the API directly.
--%>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Compare two loans (Vue)</title>
    <link href="Content/site.css" rel="stylesheet" />
    <script src="Scripts/loan-compare/loan-compare.js" defer></script>
</head>
<body>
    <main class="page">
        <header class="page-header">
            <h1>Compare two loans</h1>
            <p class="muted">Vue version: a custom element calls the API. <a href="Default.aspx">Web Forms version</a></p>
        </header>

        <loan-compare api-base="<%: System.Configuration.ConfigurationManager.AppSettings["LoanApiBaseUrl"] %>">
            <noscript>This calculator needs JavaScript. Use the <a href="Default.aspx">Web Forms version</a> instead.</noscript>
        </loan-compare>
    </main>
</body>
</html>
