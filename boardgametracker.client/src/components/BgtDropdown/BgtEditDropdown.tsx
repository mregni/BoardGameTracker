import { useTranslation } from 'react-i18next';
import { ComponentPropsWithoutRef, ReactNode, useState } from 'react';
import { cx } from 'class-variance-authority';
import * as DropdownMenu from '@radix-ui/react-dropdown-menu';

import { BgtIcon } from '../BgtIcon/BgtIcon';

import TrashIcon from '@/assets/icons/trash.svg?react';
import PencilIcon from '@/assets/icons/pencil.svg?react';
import DotsVertical from '@/assets/icons/dots-vertical.svg?react';
import DotsHorizontal from '@/assets/icons/dots-horizontal.svg?react';

interface Props extends ComponentPropsWithoutRef<'div'> {
  triggerClassName?: string;
  contentClassName?: string;
  icon: ReactNode;
  onDelete: () => void;
  onEdit: () => void;
}

const BgtEditDropdown = (props: Props) => {
  const { className, triggerClassName, contentClassName, icon, onDelete, onEdit } = props;
  const { t } = useTranslation();

  const [open, setOpen] = useState(false);

  return (
    <div className={className}>
      <DropdownMenu.Root open={open}>
        <DropdownMenu.Trigger
          className={cx(triggerClassName, 'focus-visible:outline-none flex items-end')}
          onClick={() => setOpen(true)}
        >
          <BgtIcon icon={icon} className="size-5" />
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
              onClick={() => {
                setOpen(false);
                onEdit();
              }}
              className="leading-none rounded-md flex items-center gap-3 p-2 select-none outline-none data-[highlighted]:bg-primary"
            >
              <PencilIcon className="size-5" /> <span className="first-letter:uppercase">{t('common.edit')}</span>
            </DropdownMenu.Item>
            <DropdownMenu.Item
              onClick={() => {
                setOpen(false);
                onDelete();
              }}
              className="leading-none rounded-[3px] flex items-center gap-3 p-2 select-none outline-none data-[highlighted]:bg-primary data-[highlighted]:text-red-500 text-red-500"
            >
              <TrashIcon className="size-5" />{' '}
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
      contentClassName="will-change-[opacity,transform] items-end"
      className={className}
      icon={<DotsVertical className="size-4" />}
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
      className={className}
      icon={<DotsHorizontal className="size-5" />}
    />
  );
};
