import { useTranslation } from 'react-i18next';
import { memo } from 'react';

import { CreateGameSchema } from '@/models';
import { type AnyReactForm, BgtFormField, BgtInputField } from '@/components/BgtForm';

interface GameFormBasicFieldsProps {
  form: AnyReactForm;
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
