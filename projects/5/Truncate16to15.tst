// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.

load Truncate16to15.hdl,
output-file Truncate16to15.out,
compare-to Truncate16to15.cmp,
output-list in%B1.16.1 out%B1.15.1;

set in %B0000000000000000,
eval,
output;

set in %B1111111111111111,
eval,
output;

set in %B1010101010101010,
eval,
output;

set in %B0011110011000011,
eval,
output;

set in %B0001001000110100,
eval,
output;