@{
    RootModule = 'WindowsPreventer.psm1'
    ModuleVersion = '0.1.0'
    GUID = '3b8bd0d9-02a6-4809-9086-9e3198823e85'
    Author = 'Windows Preventer'
    CompanyName = 'Windows Preventer'
    Description = 'Applies documented Windows policy restrictions to selected non-admin profiles.'
    PowerShellVersion = '5.1'
    FunctionsToExport = @('Invoke-WindowsPreventerPolicy', 'Test-WindowsPreventerAdministrator')
    CmdletsToExport = @()
    VariablesToExport = '*'
    AliasesToExport = @()
}