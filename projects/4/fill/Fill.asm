// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.

// Runs an infinite loop that listens to the keyboard input. 
// When a key is pressed (any key), the program blackens the screen,
// i.e. writes "black" in every pixel. When no key is pressed, 
// the screen should be cleared.

//// Replace this comment with your code.
@count
M=0 // count = 0

(LOOP)

@KBD
D=M
@CLEAR
D;JLE // if key <=0, clear, else fill

(FILL)
@count
D=M
M=M+1 // count++
@SCREEN
A=D+A // D -> pixel
M=65535 // set pixel[count] = 1
@LOOP
0;JMP

(CLEAR)
@count
M=M-1
D=M
@SCREEN
A=D+A
M=0
@CLEAR
D;JGE
@LOOP
0;JMP


(END)
0;JMP