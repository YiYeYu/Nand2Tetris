// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.

// Multiplies R0 and R1 and stores the result in R2.
// (R0, R1, R2 refer to RAM[0], RAM[1], and RAM[2], respectively.)
// The algorithm is based on repetitive addition.

//// Replace this comment with your code.
@R2
M=0 // R2 = 0
@R1
D=M // D = R1, as count

(LOOP)
@END
D;JLE // end if count <= 0

@R0
D=M // D = R0
@R2
M=D+M // R2 = R2 + D(R0)

@R1
MD=M-1 // R1--
@LOOP
0;JMP

(END)
0;JMP
