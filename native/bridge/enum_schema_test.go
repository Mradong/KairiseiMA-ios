package main

import "testing"

func TestNativeEnumNameAmbiguity(t *testing.T) {
	tests := []struct {
		name     string
		families []nativeEnum
		wantErr  bool
	}{
		{"legacy version", []nativeEnum{{Name: "ORDER", Values: []nativeEnumValue{{Name: "SELF_ENEMY_AI_VAR", Value: 110}}}}, false},
		{"modern version", []nativeEnum{{Name: "ORDER", Values: []nativeEnumValue{{Name: "SELF_STYLE", Value: 110}, {Name: "SELF_ENEMY_AI_VAR", Value: 111}}}}, false},
		{"mixed versions", []nativeEnum{{Name: "ORDER", Values: []nativeEnumValue{{Name: "SELF_ENEMY_AI_VAR", Value: 110}, {Name: "SELF_ENEMY_AI_VAR", Value: 111}}}}, true},
		{"duplicate entry", []nativeEnum{{Name: "ORDER", Values: []nativeEnumValue{{Name: "X", Value: 1}, {Name: "X", Value: 1}}}}, true},
		{"same number different names", []nativeEnum{{Name: "ORDER", Values: []nativeEnumValue{{Name: "X", Value: 1}, {Name: "Y", Value: 1}}}}, true},
		{"separate families", []nativeEnum{{Name: "ONE", Values: []nativeEnumValue{{Name: "X", Value: 1}}}, {Name: "TWO", Values: []nativeEnumValue{{Name: "X", Value: 2}}}}, false},
	}
	for _, tc := range tests {
		t.Run(tc.name, func(t *testing.T) {
			_, err := validateNativeEnums(tc.families)
			if (err != nil) != tc.wantErr {
				t.Fatalf("validateNativeEnums error = %v, want error = %v", err, tc.wantErr)
			}
		})
	}
}
