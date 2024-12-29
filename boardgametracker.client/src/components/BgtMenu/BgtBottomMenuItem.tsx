import { Link } from 'react-router-dom';

import { BgtIcon } from '../BgtIcon/BgtIcon';
import { MenuItem } from '../../models';

export interface Props {
  item: MenuItem;
}
export const BgtBottomMenuItem = (props: Props) => {
  const { item } = props;

  return (
    <Link to={item.path} className="grow cursor-pointer text-white hover:bg-sky-800">
      <div className="flex flex-row justify-center pt-5 pb-3">
        <BgtIcon icon={item.icon} />
      </div>
    </Link>
  );
};
