#! isa:ss_asm version:1 instruction_width:2 entry:main
#! output_format:digital_eeprom

zero:0x00
halt:0xFF


main:
	reset_register
	loop_forward
	loop_backward
	halt
	
reset_register:
	ldi r0, zero
	return
	
loop_forward:
	ldi r0, 0x01
	ldi r0, 0x02
	ldi r0, 0x04
	ldi r0, 0x08
	ldi r0, 0x10
	ldi r0, 0x20
	ldi r0, 0x40
	ldi r0, 0x80
	return
	
loop_backward:
	ldi r0, 0x80
	ldi r0, 0x40
	ldi r0, 0x20
	ldi r0, 0x10
	ldi r0, 0x08
	ldi r0, 0x04
	ldi r0, 0x02
	ldi r0, 0x01
	return

halt:
	halt
	
	

	
