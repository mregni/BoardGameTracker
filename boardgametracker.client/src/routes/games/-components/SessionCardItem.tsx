import { memo, useMemo } from 'react';
import { format } from 'date-fns';
import { useNavigate } from '@tanstack/react-router';

import { StringToHsl } from '@/utils/stringUtils';
import { usePlayerById } from '@/routes/-hooks/usePlayerById';
import { Session } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';

interface Props {
  session: Session;
  dateFormat: string;
}

const SessionCardItemComponent = (props: Props) => {
  const { session, dateFormat } = props;
  const { playerById } = usePlayerById();
  const navigate = useNavigate();

  const winner = useMemo(() => {
    const winners = session.playerSessions.filter((ps) => ps.won);
    if (winners.length === 0) return null;
    return winners.length === 0 ? null : playerById(winners[0].playerId);
  }, [session, playerById]);

  const winnerSession = useMemo(() => {
    const winners = session.playerSessions.filter((ps) => ps.won);
    if (winners.length === 0) return null;
    return winners.length === 0 ? null : winners[0];
  }, [session]);

  return (
    <div className="flex items-center gap-4 bg-primary/5 rounded-lg p-4 border border-primary/10">
      <div className="w-10 h-10 rounded-full overflow-hidden bg-primary/20 border border-primary/30 shrink-0">
        {winner && (
          <BgtAvatar
            onClick={() => navigate({ to: `/players/${winner.id}` })}
            image={winner.image}
            title={winner.name}
            color={StringToHsl(playerById(winner.id)?.name)}
            size="large"
          />
        )}
      </div>
      <div className="flex-1">
        <BgtText color="white">{winner?.name}</BgtText>
        <div className="text-white/50 text-sm">{format(session.start, dateFormat)}</div>
      </div>
      <div className="text-right">
        <BgtText color="cyan" weight="bold">
          {winnerSession?.score} pts
        </BgtText>
        <div className="text-white/50 text-sm">
          {session.playerSessions.length}p • {session.minutes}m
        </div>
      </div>
    </div>
  );
};

SessionCardItemComponent.displayName = 'SessionCardItem';

export const SessionCardItem = memo(SessionCardItemComponent);
