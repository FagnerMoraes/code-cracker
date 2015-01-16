﻿Imports CodeCracker.Test.TestHelper
Imports Xunit


Public Class StringBuilderLoopTests
    Inherits CodeFixTest(Of StringBuilderInLoopAnalyzer, StringBuilderInLoopCodeFixProvider)

    <Fact>
    Public Async Function WhileWithoutAddAssignmentExpressionDoesNotCreateDiagnostic() As Task
        Const source = "
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            While (Now.Second Mod 2 = 0)
                Method()
            End While
        End Sub
        Public Sub Method()

        End Sub
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(source)
    End Function

    <Fact>
    Public Async Function WhileWithoutStringConcatDoesNotCreateDiagnostic() As Task
        Dim source = "
Dim a = 0
While A < 10
    a += 1
End While".WrapInMethod()

        Await VerifyBasicHasNoDiagnosticsAsync(source)
    End Function

    <Fact>
    Public Async Function WhileWithStringConcatOnLocalVariableCreateDiagnostic() As Task
        Dim source = "
Dim a = """"
While Now.Second mod 2 = 10
    a += """"
End While".WrapInMethod()

        Dim expected = GetExpected()

        Await VerifyBasicDiagnosticsAsync(source, expected)
    End Function

    <Fact>
    Public Async Function WhileWithStringConcatOnFieldVariableCreatesDiagnostic() As Task
        Const source = "
Namespace ConsoleApplication1
    Class TypeName
        Private a As String = """"
        Public Sub Foo()
            While (Now.Second Mod 2 = 0)
                a += """"
            End While
        End Sub
    End Class
End Namespace"

        Dim expected As DiagnosticResult = GetExpected()
        expected.Locations(0).Line = 7
        expected.Locations(0).Column = 17
        Await VerifyBasicDiagnosticsAsync(source, expected)
    End Function

    Private Shared Function GetExpected() As DiagnosticResult
        Return New DiagnosticResult With {
            .Id = PerformanceDiagnostics.StringBuilderInLoop,
            .Message = String.Format(New StringBuilderInLoopAnalyzer().MsgFormat, "a"),
            .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 9, 5)}
        }
    End Function

    <Fact>
    Public Async Function WhileWithStringConcatOnLocalVariableCreatesDiagnostic() As Task
        Dim source = "
Dim a = """"
While Now.Second mod 2 = 0
    a += """"
End While
".WrapInMethod()

        Await VerifyBasicDiagnosticsAsync(source, GetExpected())
    End Function

    <Fact>
    Public Async Function WhileWithStringConcatOnPropertyVariableCreatesDiagnostic() As Task
        Const source = "
Namespace ConsoleApplication1
    Class TypeName
        Private Property a As String = """"
        Public Sub Foo()
            While (Now.Second Mod 2 = 0)
                a += """"
            End While
        End Sub
    End Class
End Namespace"

        Dim expected As DiagnosticResult = GetExpected()
        expected.Locations(0).Line = 7
        expected.Locations(0).Column = 17

        Await VerifyBasicDiagnosticsAsync(source, expected)
    End Function
    <Fact>
    Public Async Function WhileWithStringConcatWithSeveralConcatsOnDifferentVarsCreatesSeveralDiagnostics() As Task
        Dim source = "
Dim a = """"
Dim myString2 = """"
While Now.Second mod 2 = 0
    a += """"
    myString2 += """"
End While
Console.WriteLine(myString2)
".WrapInMethod()

        Dim expected1 As New DiagnosticResult With {
                .Id = PerformanceDiagnostics.StringBuilderInLoop,
                .Message = String.Format(New StringBuilderInLoopAnalyzer().MsgFormat, "a"),
                .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
                .Locations = {New DiagnosticResultLocation("Test0.vb", 10, 5)}
            }

        Dim expected2 As New DiagnosticResult With {
                .Id = PerformanceDiagnostics.StringBuilderInLoop,
                .Message = String.Format(New StringBuilderInLoopAnalyzer().MsgFormat, "myString2"),
                .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
                .Locations = {New DiagnosticResultLocation("Test0.vb", 11, 5)}
            }

        Await VerifyBasicDiagnosticsAsync(source, expected1, expected2)
    End Function

    <Fact>
    Public Async Function WhileWithStringConcatWithSimpleAssignmentCreatesDiagnostic() As Task
        Dim source = "
Dim a = """"
While Now.Second mod 2 = 0
    a = a + """"
End While
".WrapInMethod()

        Await VerifyBasicDiagnosticsAsync(source, GetExpected())
    End Function

    <Fact>
    Public Async Function WhileWithStringConcatWithSimpleAssignmentOnDifferentDimDoesNotCreateDiagnostic() As Task
        Dim source = "
Dim a = """"
Dim otherString = """"
While Now.Second Mod 2 = 0
    a = otherString + """"
End While
".WrapInMethod
        Await VerifyBasicHasNoDiagnosticsAsync(source)
    End Function

    <Fact>
    Public Sub FixesAddAssignmentInWhile()
        Dim source = "
Dim a = """"
While Now.Second Mod 2 = 0
    a += ""a""
End While
".WrapInMethod

        Dim fix = "
Dim a = """"
Dim builder As New System.Text.StringBuilder()
builder.Append(a)
While Now.Second Mod 2 = 0
    builder.Append(""a"")
End While
a = builder.ToString()
".WrapInMethod()
        VerifyBasicFix(source, fix)
    End Sub

    <Fact>
    Public Sub FixesAddAssignmentInWhileWithSystemTextInContext()
        Dim source = "
Imports System.Text
Namespace ConsoleApplication1

    Class TypeName
        Public Sub Foo()
            Dim a = """"
            While (Now.Second Mod 2 = 0)
                a += ""a""
            End While
            Console.WriteLine(a)
        End Sub
    End Class
End Namespace".WrapInMethod


        Dim fix = "
Imports System.Text
Namespace ConsoleApplication1

    Class TypeName
        Public Sub Foo()
            Dim a = """"
            Dim builder As New StringBuilder()
            builder.Append(a)
            While (Now.Second Mod 2 = 0)
                builder.Append(""a"")
            End While
            a = builder.ToString()
        End Sub
    End Class
End Namespace".WrapInMethod

        VerifyBasicFix(source, fix)
    End Sub

    <Fact>
    Public Sub FixesSimpleAssignmentInWhile()
        Dim source = "            
            Dim a = """"
            ' comment 3
            While (Now.Second Mod 2 = 0)
                ' comment 1
                a += ""a"" 'comment 2
            End While 'comment 4
".WrapInMethod

        Dim fix = "            
            Dim a = """"
            Dim builder As New System.Text.StringBuilder()
            builder.Append(a)
            ' comment 3
            While (Now.Second Mod 2 = 0)
                ' comment 1
                builder.Append(""a"") 'comment 2
            End While 'comment 4
            a = builder.ToString()
".WrapInMethod

        VerifyBasicFix(source, fix)
    End Sub

    <Fact>
    Public Sub FixesAddAssignmentWhenThereAre2WhilesOnBlock()
        Dim source = "            
            Dim a = """"
            While (Now.Second Mod 2 = 0)
                Dim a = 1
            End While
            While (Now.Second Mod 2 = 0)
                a += ""a""
            End While
".WrapInMethod()

        Dim fix = "
            Dim a = """"
            While (Now.Second Mod 2 = 0)
                Dim a = 1
            End While
            Dim builder As New System.Text.StringBuilder()
            builder.Append(a)
            While (Now.Second Mod 2 = 0)
                builder.Append(""a"")
            End While
            a = builder.ToString()
".WrapInMethod()

        VerifyBasicFix(source, fix)
    End Sub

    <Fact>
    Public Sub FixesAddAssignmentWithoutClashingTheBuilderName()
        Dim source = "
            Dim builder = 1
            Dim a = """"
            While (Now.Second Mod 2 = 0)
                a += ""a""
            End While
".WrapInMethod()

        Dim fix = "
            Dim builder = 1
            Dim a = """"
            Dim builder1 As New System.Text.StringBuilder()
            builder1.Append(a)
            While (Now.Second Mod 2 = 0)
                builder1.Append(""a"")
            End While
            a = builder1.ToString()
".WrapInMethod()

        VerifyBasicFix(source, fix)
    End Sub

    <Fact>
    Public Sub FixesAddAssignmentWithoutClashingTheBuilderNameOnAField()
        Dim source = "
Namespace ConsoleApplication1

    Class TypeName
        Private builder As Intever
        Public Sub Foo()
            Dim builder = 1
            Dim a = """"
            While (Now.Second Mod 2 = 0)
                a += ""a""
            End While
        End Sub
    End Class
End Namespace".WrapInMethod


        Dim fix = "
Namespace ConsoleApplication1

    Class TypeName
        Private builder As Integer
        Public Sub Foo()
            builder = 1
            Dim a = """"
            Dim builder1 As New System.Text.StringBuilder()
            builder1.Append(a)
            While (Now.Second Mod 2 = 0)
                builder1.Append(""a"")
            End While
            a = builder1.ToString()
        End Sub
    End Class
End Namespace".WrapInMethod

        VerifyBasicFix(source, fix)
    End Sub

    <Fact>
    Public Sub ForWithStringConcatOnLocalVariableCreatesDiagnostic()
        Dim source = "
            Dim a = """"
            For i As Integer = 1 To 10
                a += ""a""
            Next".WrapInMethod

        Dim fix = "
            Dim a = """"
            Dim builder As New System.Text.StringBuilder()
            builder.Append(a)
            For i As Integer = 1 To 10
                builder.Append(""a"")
            Next
            a = builder.ToString()".WrapInMethod

        VerifyBasicFix(source, fix)
    End Sub

    <Fact>
    Public Sub FixesAddAssignmentInFor()
        Dim source = "
            Dim a = """"
            For i As Integer = 1 To 10
                a += ""b""
                Exit For
            Next".WrapInMethod

        Dim builder As New System.Text.StringBuilder()

        Dim fix = "
            Dim a = """"
            Dim builder As New System.Text.StringBuilder()
            builder.Append(a)
            For i As Integer = 1 To 10
                builder.Append(""a"")
                Exit For
            Next
            a = builder.ToString()".WrapInMethod

        VerifyBasicFix(source, fix)
    End Sub

    <Fact>
    Public Async Function ForeachWithStringConcatOnLocalVariableCreatesDiagnostic() As Task
        Dim source = "
            Dim a = ""
            For Each i In {1, 2, 3}
                a += """"
            Next".WrapInMethod

        Dim expected As New DiagnosticResult With
            {
            .Id = PerformanceDiagnostics.StringBuilderInLoop,
            .Message = String.Format(New StringBuilderInLoopAnalyzer().MsgFormat, "a"),
            .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 14, 21)}
        }
        Await VerifyBasicDiagnosticsAsync(source, expected)

    End Function

    <Fact>
    Public Sub FixesAddAssignmentInForEach()
        Dim source = "
            Dim a = """"
            For Each i In {1, 2, 3}
                a += """"
            Next".WrapInMethod

        Dim fix = "
            Dim a = """"
            Dim builder As New System.Text.StringBuilder()
            builder.Append(a)
            For Each i In {1, 2, 3}
                builder.Append(""a"")
            Next
            a = builder.ToString()".WrapInMethod

        VerifyBasicFix(source, fix)
    End Sub

    <Fact>
    Public Async Function DoWithStringConcatOnLocalVariableCreatesDiagnostic() As Task
        Dim source = "
            Dim a = """"
            Do
                a += """"
            Loop Until Now.Second Mod 2 = 0
".WrapInMethod

        Dim expected As New DiagnosticResult With
            {
            .Id = PerformanceDiagnostics.StringBuilderInLoop,
            .Message = String.Format(New StringBuilderInLoopAnalyzer().MsgFormat, "a"),
            .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 9, 17)}
        }
        Await VerifyBasicDiagnosticsAsync(source, expected)
    End Function

    <Fact>
    Public Sub FixesAddAssignmentInForDo()
        Dim source = "
            Dim a = """"
            Do
                a += ""a""
            Loop Until Now.Second Mod 2 = 0
".WrapInMethod

        Dim b As New System.Text.StringBuilder()

        Dim fix = "
            Dim a = """"
            Dim builder As New System.Text.StringBuilder()
            builder.Append(a)
            Do
                builder.Append(""a"")
            Loop Until Now.Second Mod 2 = 0
            a = builder.ToString()
".WrapInMethod

        VerifyBasicFix(source, fix)
    End Sub
End Class

'Imports System.Text
'Namespace ConsoleApplication1

'    Class TypeName
'        Public Sub Foo()
'            Dim a = ""
'            Do
'                a += ""
'            Loop Until Now.Second Mod 2 = 0
'        End Sub
'    End Class
'End Namespace