import { BgtText } from '@/components/BgtText/BgtText';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import BgtButton from '@/components/BgtButton/BgtButton';
import Plus from '@/assets/icons/plus.svg?react';
import Gamepad from '@/assets/icons/gamepad.svg?react';

interface GameDetailEmptyStateProps {
  onLogSession: () => void;
}

export const GameDetailEmptyState = ({ onLogSession }: GameDetailEmptyStateProps) => {
  return (
    <div className="flex items-center justify-center min-h-[60vh]">
      <div className="text-center max-w-lg flex flex-col gap-3">
        <div className="mb-6 flex justify-center">
          <div className="w-24 h-24 rounded-full bg-primary/10 border-2 border-primary/30 flex items-center justify-center">
            <Gamepad className="text-primary/50 size-12" />
          </div>
        </div>

        <BgtHeading className="mb-3">No Sessions Logged Yet</BgtHeading>

        <BgtText color="white" opacity={60} className="mb-8 leading-relaxed">
          Start tracking your game sessions to see detailed statistics, player performance, and session history. Log
          your first session to unlock all the analytics!
        </BgtText>

        <BgtButton onClick={onLogSession} className="mt-6">
          <Plus /> Log Your First Session
        </BgtButton>

        <BgtText color="white" opacity={40} size="2" className="pt-6">
          Session data includes players, scores, duration, and more
        </BgtText>
      </div>
    </div>
  );
};
