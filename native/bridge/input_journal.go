package main

import (
	"fmt"
	"math"
	"strconv"

	"kairisei.local/server/battlecore"
)

type inputJournal struct {
	data, pending []byte
	starting      bool
	err           error
}

func (j *inputJournal) append(view battlecore.StateView, opcode int, args ...int) {
	if j.err != nil || view.Turn > 99 {
		return
	}
	if len(args) > 20 {
		j.err = fmt.Errorf("too many input record fields")
		return
	}
	var hp int64
	for _, member := range view.Members {
		if member.MemberType >= 1 && member.MemberType <= 4 && member.HP > 0 {
			hp += int64(member.HP)
		}
	}
	j.pending = strconv.AppendInt(j.pending, hp, 10)
	j.pending = append(j.pending, ',')
	j.pending = strconv.AppendInt(j.pending, int64(opcode), 10)
	for i := 0; i < 20; i++ {
		value := int64(math.MaxInt32)
		if i < len(args) {
			value = int64(args[i])
			if value < math.MinInt32 || value > math.MaxInt32 {
				j.err = fmt.Errorf("input argument exceeds int32")
				return
			}
		}
		j.pending = append(j.pending, ',')
		j.pending = strconv.AppendInt(j.pending, value, 10)
	}
	j.pending = append(j.pending, '\n')
	base := len(j.data)
	if j.starting {
		base = 0
	}
	if base+len(j.pending) > maxSessionOutput {
		j.err = fmt.Errorf("client input report exceeds 1 MiB")
	}
}

func (j *inputJournal) begin(view battlecore.StateView, operation, member, enemyType, reservedSlot int, selection battlecore.StandaloneCardPlay, burst battlecore.BurstSelection) error {
	j.pending = nil
	j.err = nil
	j.starting = operation == 1
	switch operation {
	case 1:
		j.append(view, 22, enemyType)
	case 2:
		j.append(view, 10)
	case 3:
		j.append(view, 0)
	case 4:
		args := make([]int, 0, 20)
		args = append(args, selection.MemberType)
		args = append(args, selection.CardTypes[:]...)
		args = append(args, selection.Targets[:]...)
		args = append(args, selection.SphereSlot, selection.SphereTarget)
		targets := append(append([]int(nil), selection.Targets[:]...), selection.SphereTarget)
		for _, target := range targets {
			arthur := 0
			for _, value := range view.Members {
				if value.MemberType == target {
					arthur = value.ArthurType
					break
				}
			}
			args = append(args, arthur)
		}
		args = append(args, selection.SummonsCardMask)
		j.append(view, 2, args...)
	case 5:
		j.append(view, 6)
	case 6:
		j.append(view, 1)
	case 7, 14:
	case 8:
		j.append(view, 29)
	case 9:
		j.append(view, 30)
	case 11:
		j.append(view, 11, member)
	case 12:
		j.append(view, 28, member, reservedSlot)
	case 13:
		j.append(view, 7, member)
	case 15:
		args := []int{member, burst.Target, burst.SkillSlot}
		args = append(args, burst.CardTypes[:]...)
		j.append(view, 34, args...)
	default:
		return fmt.Errorf("input operation is not connected")
	}
	return j.err
}

func (j *inputJournal) deckChanged(change battlecore.DeckOrderChange) {
	opcode := 27
	if change.Opening || j.starting {
		opcode = 25
	}
	args := []int{change.MemberType}
	args = append(args, change.Order[:]...)
	j.append(change.State, opcode, args...)
}

func (j *inputJournal) commit() error {
	if j.err != nil {
		return j.err
	}
	if j.starting {
		j.data = nil
	}
	j.data = append(j.data, j.pending...)
	j.pending = nil
	return nil
}
