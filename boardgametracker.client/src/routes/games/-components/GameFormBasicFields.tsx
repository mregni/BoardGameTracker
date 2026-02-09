import { useTranslation } from 'react-i18next';
import { memo } from 'react';

import { CreateGameSchema } from '@/models';
import { BgtFormField, BgtInputField } from '@/components/BgtForm';

interface GameFormBasicFieldsProps {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  form: any;
  disabled: boolean;
}

const GameFormBasicFieldsComponent = ({ form, disabled }: GameFormBasicFieldsProps) => {
  const { t } = useTranslation();

  return (
    <BgtFormField form={form} name="title" schema={CreateGameSchema}>
      {(field) => (
        <BgtInputField field={field} type="text" disabled={disabled} label={t('game.new.manual.game-title.label')} />
      )}
    </BgtFormField>
  );
};

GameFormBasicFieldsComponent.displayName = 'GameFormBasicFields';

export const GameFormBasicFields = memo(GameFormBasicFieldsComponent);
