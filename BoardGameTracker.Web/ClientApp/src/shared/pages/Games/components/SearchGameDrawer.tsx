import {Button, Drawer, Form, Input, Select, Space} from 'antd';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {GcDrawer} from '../../../components/GcDrawer';
import {
  BggIdSearch, GameState, getItemStateTranslationKey, SearchResultType,
} from '../../../models';
import {GamesContext} from '../context';
import useSearchGame from '../useSearchGame';

interface Props {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
}

const SearchGameDrawer = (props: Props) => {
  const { loadGames } = useContext(GamesContext);
  const { setOpen, open } = props;
  const { t } = useTranslation();
  const { search, searching } = useSearchGame();
  const [form] = Form.useForm();
  const screens = useBreakpoint();

  const onClose = () => {
    form.resetFields();
    setOpen(false);
  };

  const triggerGameSearch = async (values: BggIdSearch) => {
    const result = await search(values.bggId, values.state);
    if (![SearchResultType.Failed, SearchResultType.NotFound].includes(result)) {
      loadGames();
      onClose();
    }
  }

  const initialValues = {
    bggId: '',
    state: GameState.Owned
  }

  return (
    <GcDrawer
      title={t('games.new.title')}
      open={open} 
      setOpen={setOpen}>
      <Form
        form={form}
        labelCol={{ span: 4 }}
        wrapperCol={{ span: 19 }}
        layout="horizontal"
        autoComplete='off'
        hideRequiredMark
        onFinish={triggerGameSearch}
        initialValues={initialValues}
      >
        <Form.Item
          label={t('games.new.bgg.label')}
          style={{ width: '100%' }}
          name="bggId"
          rules={[{ required: true, message: t('games.new.bgg.required') }]}
        >
          <Input placeholder={t('games.new.bgg.placeholder')} disabled={searching} />
        </Form.Item>
        <Form.Item
          label={t('common.state')}
          style={{ width: '100%' }}
          name="state"
        >
          <Select
            disabled={searching}
            options={Object
              .keys(GameState)
              .filter(value => !isNaN(Number(value)))
              .map((value) => ({ label: t(getItemStateTranslationKey(value)), value: Number(value) }))}
          >
          </Select>
        </Form.Item>
        <Form.Item wrapperCol={{ offset: 1, span: 22 }}>
          <Button
            type="primary"
            htmlType="submit"
            loading={searching}
            block
          >
            {t('games.new.bgg.search')}
          </Button>
        </Form.Item>
      </Form>
    </GcDrawer>
  );
}

export default SearchGameDrawer