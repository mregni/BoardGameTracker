import { useTranslation } from 'react-i18next';
import { use } from 'i18next';
import { cx } from 'class-variance-authority';
import { useNavigate } from '@tanstack/react-router';

import { BgtPoster } from '@/routes/-components/BgtPoster';
import { Player } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';

interface Props {
  title: string;
  count: number;
  players: Player[];
  variant: 'accepted' | 'declined' | 'pending';
}

const variantStyles = {
  accepted: {
    container: 'bg-green-500/10 border-green-500/20',
    title: 'text-green-400',
    badge: 'bg-green-500/20 text-green-300',
  },
  declined: {
    container: 'bg-red-500/10 border-red-500/20',
    title: 'text-red-400',
    badge: 'bg-red-500/20 text-red-300',
  },
  pending: {
    container: 'bg-yellow-500/10 border-yellow-500/20',
    title: 'text-yellow-400',
    badge: 'bg-yellow-500/20 text-yellow-300',
  },
};

export const RsvpSection = (props: Props) => {
  const { title, count, players, variant } = props;
  const { t } = useTranslation();
  const navigate = useNavigate();
  const styles = variantStyles[variant];

  return (
    <div className={cx('border rounded-lg p-3', styles.container)}>
      <div className="flex items-center gap-2 mb-2">
        <span className={cx('text-sm font-medium', styles.title)}>
          {title} ({count})
        </span>
      </div>
      <div className="flex flex-wrap gap-1">
        {players.length === 0 ? (
          <BgtText size="1" color="gray">
            {t('common.none')}
          </BgtText>
        ) : (
          players.map((player) => (
            <div key={player.id} className={cx('flex items-center gap-2 pl-1 pr-2 py-1 rounded text-xs', styles.badge)}>
              <BgtAvatar
                key={player.id}
                title={player.name}
                image={player.image}
                onClick={() => navigate({ to: `/players/${player.id}` })}
                withTitle
              />
            </div>
          ))
        )}
      </div>
    </div>
  );
};
