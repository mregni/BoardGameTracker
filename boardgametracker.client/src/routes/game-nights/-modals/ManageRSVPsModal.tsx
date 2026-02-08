import { useTranslation } from 'react-i18next';
import { cx } from 'class-variance-authority';

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
      status,
    });
  };

  const handleClose = () => {
    setOpen(false);
  };

  const acceptedCount = gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Accepted).length;
  const pendingCount = gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Pending).length;
  const declinedCount = gameNight.invitedPlayers.filter((x) => x.state === GameNightRsvpState.Declined).length;

  return (
    <BgtDialog open={open}>
      <BgtDialogContent className="max-w-3xl!">
        <div className="flex items-center justify-between mb-6">
          <div>
            <BgtDialogTitle>{t('game-nights.manage-rsvps')}</BgtDialogTitle>
            <BgtText size="2" color="gray" className="mt-1">
              {gameNight.title}
            </BgtText>
          </div>
          <button onClick={handleClose} className="p-2 hover:bg-white/10 rounded-lg transition-colors">
            <BgtText size="4">x</BgtText>
          </button>
        </div>

        <div className="mb-4 bg-blue-500/10 border border-blue-500/20 rounded-lg p-4">
          <BgtText size="2" color="blue">
            <strong>{t('game-nights.rsvp.manual-note-title')}</strong> {t('game-nights.rsvp.manual-note')}
          </BgtText>
        </div>

        <div className="space-y-3 max-h-[50vh] overflow-y-auto">
          {gameNight.invitedPlayers.map((rsvp) => (
            <div key={rsvp.id} className="bg-white/5 border border-white/10 rounded-lg p-4">
              <div className="flex items-center justify-between gap-4 flex-wrap">
                <div className="flex items-center gap-3">
                  <BgtAvatar title={rsvp.player.name} image={rsvp.player.image} />
                  <div>
                    <BgtText size="3" weight="medium">
                      {rsvp.player.name}
                    </BgtText>
                    <BgtText size="1" color="gray">
                      {t('game-nights.rsvp.current')}:{' '}
                      <span
                        className={cx(
                          rsvp.state === GameNightRsvpState.Accepted && 'text-green-400',
                          rsvp.state === GameNightRsvpState.Declined && 'text-red-400',
                          rsvp.state === GameNightRsvpState.Pending && 'text-yellow-400'
                        )}
                      >
                        {t(`game-nights.rsvp.${rsvp.state}`)}
                      </span>
                    </BgtText>
                  </div>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => handleStatusChange(rsvp.id, GameNightRsvpState.Accepted)}
                    disabled={isLoading}
                    className={cx(
                      'px-4 py-2 rounded-lg text-sm font-medium transition-colors',
                      rsvp.state === GameNightRsvpState.Accepted
                        ? 'bg-green-500 text-white'
                        : 'bg-green-500/20 text-green-300 hover:bg-green-500/30 border border-green-500/30'
                    )}
                  >
                    {t('game-nights.rsvp.accept')}
                  </button>
                  <button
                    onClick={() => handleStatusChange(rsvp.id, GameNightRsvpState.Pending)}
                    disabled={isLoading}
                    className={cx(
                      'px-4 py-2 rounded-lg text-sm font-medium transition-colors',
                      rsvp.state === GameNightRsvpState.Pending
                        ? 'bg-yellow-500 text-white'
                        : 'bg-yellow-500/20 text-yellow-300 hover:bg-yellow-500/30 border border-yellow-500/30'
                    )}
                  >
                    {t('game-nights.rsvp.maybe')}
                  </button>
                  <button
                    onClick={() => handleStatusChange(rsvp.id, GameNightRsvpState.Declined)}
                    disabled={isLoading}
                    className={cx(
                      'px-4 py-2 rounded-lg text-sm font-medium transition-colors',
                      rsvp.state === GameNightRsvpState.Declined
                        ? 'bg-red-500 text-white'
                        : 'bg-red-500/20 text-red-300 hover:bg-red-500/30 border border-red-500/30'
                    )}
                  >
                    {t('game-nights.rsvp.decline')}
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>

        <div className="mt-6 grid grid-cols-3 gap-4">
          <div className="bg-green-500/10 border border-green-500/20 rounded-lg p-4 text-center">
            <BgtText size="5" weight="bold" color="green">
              {acceptedCount}
            </BgtText>
            <BgtText size="2" color="gray">
              {t('common.accepted')}
            </BgtText>
          </div>
          <div className="bg-yellow-500/10 border border-yellow-500/20 rounded-lg p-4 text-center">
            <BgtText size="5" weight="bold" className="text-yellow-400">
              {pendingCount}
            </BgtText>
            <BgtText size="2" color="gray">
              {t('common.pending')}
            </BgtText>
          </div>
          <div className="bg-red-500/10 border border-red-500/20 rounded-lg p-4 text-center">
            <BgtText size="5" weight="bold" color="red">
              {declinedCount}
            </BgtText>
            <BgtText size="2" color="gray">
              {t('common.declined')}
            </BgtText>
          </div>
        </div>

        <BgtDialogClose className="mt-6">
          <BgtButton variant="primary" className="flex-1" onClick={handleClose}>
            {t('common.done')}
          </BgtButton>
        </BgtDialogClose>
      </BgtDialogContent>
    </BgtDialog>
  );
};
