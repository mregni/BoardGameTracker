import { useTranslation } from 'react-i18next';
import type { AnyFieldApi } from '@tanstack/react-form';

import { gameFormOpts } from '../-utils/gameFormOpts';

import { zodValidator } from '@/utils/zodValidator';
import { CreateGameSchema } from '@/models';
import { withForm } from '@/hooks/form';
import { BgtInputField } from '@/components/BgtForm';

export const GameFormBasicFields = withForm({
  ...gameFormOpts,
  props: {
    disabled: false,
  },
  render: function Render({ form, disabled }) {
    const { t } = useTranslation();

    return (
      <form.Field name="title" validators={zodValidator(CreateGameSchema, 'title')}>
        {(field: AnyFieldApi) => (
          <BgtInputField field={field} type="text" disabled={disabled} label={t('game.new.manual.game-title.label')} />
        )}
      </form.Field>
    );
  },
});
