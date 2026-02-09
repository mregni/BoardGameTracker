import { useTranslation } from 'react-i18next';

import { RsvpStatusButton } from '../-components/RsvpStatusButton';
import { RsvpStatBadge } from '../-components/RsvpStatBadge';

import { GameNight, GameNightRsvpState, UpdateGameNightRsvp } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtDialog, BgtDialogContent, BgtDialogClose, BgtDialogTitle } from '@/components/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';

interface Props {
  open: boolean;
  setOpen: (open: boolean) => void;
  gameNight: GameNight | null;
  onUpdateRsvp: (rsvp: UpdateGameNightRsvp) => Promise<unknown>;
  isLoading: boolean;
}

export const ManageRSVPsModal = (props: Props) => {
  const { open, setOpen, gameNight, onUpdateRsvp, isLoading } = props;
  const { t } = useTranslation();

  if (!gameNight) return null;

  const handleStatusChange = async (rsvpId: number, status: GameNightRsvpState) => {
    await onUpdateRsvp({
      id: rsvpId,
      gameNightId: gameNight.id,
      playerName: 0,
      state: status,
    });
  };

  const handleClose = () => {
    setOpen(false);
  };

  const acceptedCount = gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Accepted).length;
  const pendingCount = gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Pending).length;
  const declinedCount = gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Declined).length;

  return (
    <BgtDialog open={open} onClose={handleClose}>
      <BgtDialogContent className="max-w-3xl!">
        <BgtDialogTitle>{t('game-nights.rsvp.title')}</BgtDialogTitle>
        <div className="flex flex-col gap-4">
          <div className=" bg-blue-500/10 border border-blue-500/20 rounded-lg p-4">
            <BgtText size="2" color="blue">
              <strong>{t('game-nights.rsvp.manual-note-prefix')}</strong> {t('game-nights.rsvp.manual-note')}
            </BgtText>
          </div>

          <div className="flex flex-col gap-4">
            {[...gameNight.invitedPlayers]
              .sort((a, b) => (a.playerId === gameNight.hostId ? 1 : b.playerId === gameNight.hostId ? -1 : 0))
              .map((rsvp) => {
                const isHost = rsvp.playerId === gameNight.hostId;
                return (
                  <div key={rsvp.id} className="bg-white/5 border border-white/10 rounded-lg p-4">
                    <div className="flex items-center justify-between flex-row gap-4 flex-wrap">
                      <div className="flex items-center gap-3">
                        <BgtAvatar title={rsvp.player.name} image={rsvp.player.image} size="large" />
                        <BgtText size="3" weight="medium">
                          {rsvp.player.name}
                          {isHost && <span className="ml-2 text-xs text-gray-400">({t('game-nights.rsvp.host')})</span>}
                        </BgtText>
                      </div>
                      <div className="flex gap-2">
                        <RsvpStatusButton
                          onClick={() => handleStatusChange(rsvp.id, GameNightRsvpState.Accepted)}
                          disabled={isLoading || isHost}
                          isActive={rsvp.state === GameNightRsvpState.Accepted}
                          variant="green"
                          label={t('game-nights.rsvp.accept')}
                        />
                        <RsvpStatusButton
                          onClick={() => handleStatusChange(rsvp.id, GameNightRsvpState.Pending)}
                          disabled={isLoading || isHost}
                          isActive={rsvp.state === GameNightRsvpState.Pending}
                          variant="yellow"
                          label={t('game-nights.rsvp.maybe')}
                        />
                        <RsvpStatusButton
                          onClick={() => handleStatusChange(rsvp.id, GameNightRsvpState.Declined)}
                          disabled={isLoading || isHost}
                          isActive={rsvp.state === GameNightRsvpState.Declined}
                          variant="red"
                          label={t('game-nights.rsvp.decline')}
                        />
                      </div>
                    </div>
                  </div>
                );
              })}
          </div>

          <div className="grid grid-cols-3 gap-4 mb-6">
            <RsvpStatBadge count={acceptedCount} label={t('common.accepted')} variant="green" />
            <RsvpStatBadge count={pendingCount} label={t('common.pending')} variant="amber" />
            <RsvpStatBadge count={declinedCount} label={t('common.declined')} variant="red" />
          </div>
        </div>

        <BgtDialogClose>
          <BgtButton variant="primary" className="flex-1" onClick={handleClose}>
            {t('common.done')}
          </BgtButton>
        </BgtDialogClose>
      </BgtDialogContent>
    </BgtDialog>
  );
};
