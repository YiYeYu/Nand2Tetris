// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.

load Mux16Way.hdl,
output-file Mux16Way.out,
compare-to Mux16Way.cmp,
output-list in%B1.16.1 sel%B2.4.2 out%B2.1.2;

set in %B0001001000110100,
set sel 0,
eval,
output;

set sel 2,
eval,
output;
