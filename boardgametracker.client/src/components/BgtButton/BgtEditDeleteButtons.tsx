import { PencilIcon, TrashIcon } from '@heroicons/react/24/outline';

import { BgtIcon } from '../BgtIcon/BgtIcon';
import { BgtNormalEditDropdown } from '../BgtDropdown/BgtEditDropdown';

import BgtButton from './BgtButton';

interface Props {
  onDelete: () => void;
  onEdit: () => void;
}

export const BgtEditDeleteButtons = (props: Props) => {
  const { onDelete, onEdit } = props;

  return (
    <div>
      <BgtNormalEditDropdown onDelete={onDelete} onEdit={onEdit} className="md:hidden" />
      <div className="hidden flex-row justify-end gap-1 md:flex">
        <BgtButton variant="inline" color="primary" onClick={onEdit}>
          <BgtIcon icon={<PencilIcon />} size={19} />
        </BgtButton>
        <BgtButton variant="inline" color="error" onClick={onDelete}>
          <BgtIcon icon={<TrashIcon />} />
        </BgtButton>
      </div>
    </div>
  );
};
