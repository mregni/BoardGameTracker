import { ClockIcon, TrophyIcon } from '@heroicons/react/24/solid';

import { BgtAvatar, Props } from './BgtAvatar';

interface ComplexProps extends Props {
  firstPlay: boolean;
  won: boolean;
}

export const BgtComplexPlayerAvatar = (props: ComplexProps) => {
  const { firstPlay, won, ...rest } = props;
  return (
    <div className="relative">
      {won && <TrophyIcon className="w-4 absolute -right-1 -top-1 z-50 text-yellow-600" />}
      {firstPlay && <ClockIcon className="w-4 absolute -right-1 -bottom-1 z-50 text-green-600" />}
      <BgtAvatar {...rest} />
    </div>
  );
};
