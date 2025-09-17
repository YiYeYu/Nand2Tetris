($SYS_LABEL_START)
@256
D=A
@SP
M=D
@$SYS_LABEL_CUSTOME_START
0;JMP
($SYS_LABEL_END)
@$SYS_LABEL_END
0;JMP
($SYS_LABEL_CUSTOME_START)
// C_PUSH: argument, 1
@1
D=A
@ARG
AD=D+M
D=M
@SP
A=M
M=D
@SP
M=M+1
// C_POP: pointer, 1
@1
D=A
@3
AD=D+A
@R13
M=D
@SP
AM=M-1
D=M
@R13
A=M
M=D
// C_PUSH: constant, 0
@0
D=A
@SP
A=M
M=D
@SP
M=M+1
// C_POP: that, 0
@0
D=A
@THAT
AD=D+M
@R13
M=D
@SP
AM=M-1
D=M
@R13
A=M
M=D
// C_PUSH: constant, 1
@1
D=A
@SP
A=M
M=D
@SP
M=M+1
// C_POP: that, 1
@1
D=A
@THAT
AD=D+M
@R13
M=D
@SP
AM=M-1
D=M
@R13
A=M
M=D
// C_PUSH: argument, 0
@0
D=A
@ARG
AD=D+M
D=M
@SP
A=M
M=D
@SP
M=M+1
// C_PUSH: constant, 2
@2
D=A
@SP
A=M
M=D
@SP
M=M+1
// C_ARITHMETIC: sub, 
@SP
AM=M-1
D=M
@SP
AM=M-1
A=M
D=A-D
@SP
A=M
M=D
@SP
M=M+1
// C_POP: argument, 0
@0
D=A
@ARG
AD=D+M
@R13
M=D
@SP
AM=M-1
D=M
@R13
A=M
M=D
// C_LABEL: LOOP, 
(LOOP)
// C_PUSH: argument, 0
@0
D=A
@ARG
AD=D+M
D=M
@SP
A=M
M=D
@SP
M=M+1
// C_IF: COMPUTE_ELEMENT, 
@SP
AM=M-1
D=M
@COMPUTE_ELEMENT
D;JNE
// C_GOTO: END, 
@END
0;JMP
// C_LABEL: COMPUTE_ELEMENT, 
(COMPUTE_ELEMENT)
// C_PUSH: that, 0
@0
D=A
@THAT
AD=D+M
D=M
@SP
A=M
M=D
@SP
M=M+1
// C_PUSH: that, 1
@1
D=A
@THAT
AD=D+M
D=M
@SP
A=M
M=D
@SP
M=M+1
// C_ARITHMETIC: add, 
@SP
AM=M-1
D=M
@SP
AM=M-1
A=M
D=D+A
@SP
A=M
M=D
@SP
M=M+1
// C_POP: that, 2
@2
D=A
@THAT
AD=D+M
@R13
M=D
@SP
AM=M-1
D=M
@R13
A=M
M=D
// C_PUSH: pointer, 1
@1
D=A
@3
AD=D+A
D=M
@SP
A=M
M=D
@SP
M=M+1
// C_PUSH: constant, 1
@1
D=A
@SP
A=M
M=D
@SP
M=M+1
// C_ARITHMETIC: add, 
@SP
AM=M-1
D=M
@SP
AM=M-1
A=M
D=D+A
@SP
A=M
M=D
@SP
M=M+1
// C_POP: pointer, 1
@1
D=A
@3
AD=D+A
@R13
M=D
@SP
AM=M-1
D=M
@R13
A=M
M=D
// C_PUSH: argument, 0
@0
D=A
@ARG
AD=D+M
D=M
@SP
A=M
M=D
@SP
M=M+1
// C_PUSH: constant, 1
@1
D=A
@SP
A=M
M=D
@SP
M=M+1
// C_ARITHMETIC: sub, 
@SP
AM=M-1
D=M
@SP
AM=M-1
A=M
D=A-D
@SP
A=M
M=D
@SP
M=M+1
// C_POP: argument, 0
@0
D=A
@ARG
AD=D+M
@R13
M=D
@SP
AM=M-1
D=M
@R13
A=M
M=D
// C_GOTO: LOOP, 
@LOOP
0;JMP
// C_LABEL: END, 
(END)
// FibonacciSeries has 0 static variables, total 0 static variables
