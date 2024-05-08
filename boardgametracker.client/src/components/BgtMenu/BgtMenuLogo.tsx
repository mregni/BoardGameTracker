import clsx from 'clsx';

import logo from '../../assets/logo.png';

export interface Props {
  fullSize?: boolean;
}

export const BgtMenuLogo = (props: Props) => {
  const { fullSize = true } = props;

  return (
    <div className={clsx('h-16 w-full flex gap-2 items-center', !fullSize && 'justify-center')}>
      <img width="30" height="30" src={logo} />
      {fullSize && <div className="text-white">Boardgame name</div>}
    </div>
  );
};
