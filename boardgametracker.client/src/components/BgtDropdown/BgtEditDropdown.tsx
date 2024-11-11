import { useTranslation } from 'react-i18next';
import { ComponentPropsWithoutRef, ReactNode } from 'react';
import { cx } from 'class-variance-authority';
import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { EllipsisHorizontalIcon, EllipsisVerticalIcon, PencilIcon, TrashIcon } from '@heroicons/react/24/outline';

import { BgtIcon } from '../BgtIcon/BgtIcon';

interface Props extends ComponentPropsWithoutRef<'div'> {
  triggerClassName?: string;
  contentClassName?: string;
  size: number;
  icon: ReactNode;
  onDelete: () => void;
  onEdit: () => void;
}

const BgtEditDropdown = (props: Props) => {
  const { className, triggerClassName, contentClassName, size, icon, onDelete, onEdit } = props;
  const { t } = useTranslation();

  return (
    <div className={className}>
      <DropdownMenu.Root>
        <DropdownMenu.Trigger className={cx(triggerClassName, 'focus-visible:outline-none')}>
          <BgtIcon icon={icon} size={size} />
        </DropdownMenu.Trigger>

        <DropdownMenu.Portal>
          <DropdownMenu.Content
            className={cx(
              contentClassName,
              'bg-card-black border border-card-border p-2 rounded-md data-[side=top]:animate-slideDownAndFade data-[side=right]:animate-slideLeftAndFade data-[side=bottom]:animate-slideUpAndFade data-[side=left]:animate-slideRightAndFade'
            )}
            sideOffset={5}
          >
            <DropdownMenu.Item
              onClick={onEdit}
              className="leading-none rounded-md flex items-center gap-3 p-2 select-none outline-none data-[highlighted]:bg-primary"
            >
              <BgtIcon icon={<PencilIcon />} size={16} />{' '}
              <span className="first-letter:uppercase">{t('common.edit')}</span>
            </DropdownMenu.Item>
            <DropdownMenu.Item
              onClick={onDelete}
              className="leading-none rounded-[3px] flex items-center gap-3 p-2 select-none outline-none data-[highlighted]:bg-primary data-[highlighted]:text-red-500 text-red-500"
            >
              <BgtIcon icon={<TrashIcon />} size={16} />{' '}
              <span className="first-letter:uppercase">{t('common.delete.button')}</span>
            </DropdownMenu.Item>
          </DropdownMenu.Content>
        </DropdownMenu.Portal>
      </DropdownMenu.Root>
    </div>
  );
};

interface ExternalProps extends ComponentPropsWithoutRef<'div'> {
  onDelete: () => void;
  onEdit: () => void;
}

export const BgtHiddenEditDropdown = (props: ExternalProps) => {
  const { className, onDelete, onEdit } = props;
  return (
    <BgtEditDropdown
      triggerClassName="opacity:100 md:opacity-0 group-hover:opacity-100 data-[state=open]:opacity-100 transition-opacity duration-200"
      contentClassName="will-change-[opacity,transform]"
      size={15}
      className={className}
      icon={<EllipsisVerticalIcon />}
      onDelete={onDelete}
      onEdit={onEdit}
    />
  );
};

export const BgtNormalEditDropdown = (props: ExternalProps) => {
  const { className, onDelete, onEdit } = props;
  return (
    <BgtEditDropdown
      onEdit={onEdit}
      onDelete={onDelete}
      size={20}
      className={className}
      icon={<EllipsisHorizontalIcon />}
    />
  );
};
