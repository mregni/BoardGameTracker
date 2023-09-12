import {
  Button, Col, ConfigProvider, Dropdown, Layout, MenuProps, Row, Space, Typography,
} from 'antd';
import {ItemType} from 'antd/es/breadcrumb/Breadcrumb';
import {MenuItemType} from 'antd/es/menu/hooks/useItems';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import React, {Children, ReactElement, ReactNode} from 'react';
import {Link, useNavigate} from 'react-router-dom';

import {purple} from '@ant-design/colors';
import {ArrowLeftOutlined, MoreOutlined} from '@ant-design/icons';

import {GcNoDataLoader} from '../GcNoDataLoader';

const { Header, Content } = Layout;
const { Title } = Typography;

export interface GcMenuItem {
  buttonType: 'link' | 'text' | 'ghost' | 'default' | 'primary' | 'dashed' | undefined;
  icon: ReactNode;
  onClick?: () => void;
  to?: string;
  content: string;
}

interface HeaderProps {
  isLoading: boolean;
  title: string;
  hasBack?: boolean;
  backNavigation?: string;
  items: GcMenuItem[];
}

export const GcPageContainerHeader = (props: HeaderProps) => {
  const { isLoading, title, items, hasBack = false, backNavigation = '' } = props;
  const navigate = useNavigate();
  const screens = useBreakpoint();

  const dropdownItems: MenuProps['items'] = items.map((item, i) => { return { key: i, icon: item.icon, label: <Link to={item.to ?? ''} onClick={item.onClick}>{item.content}</Link> } })

  return (
    <Row justify="space-between">
      <Col>
        <Space wrap>
          {
            hasBack && (
              <Button
                type="text"
                icon={<ArrowLeftOutlined />}
                onClick={() => navigate(backNavigation)}
                disabled={isLoading}
              />
            )
          }
          <Title level={screens.md ? 3 : 5} style={{ margin: 0 }}>{title}</Title>
        </Space>
      </Col>
      {
        screens.lg !== undefined && (
          <Col>
            <Space wrap>
              {!screens.lg &&
                <Dropdown menu={{ items: dropdownItems }} placement="bottomRight" arrow={{ pointAtCenter: true }}>
                  <Button icon={<MoreOutlined />} type='ghost'></Button>
                </Dropdown>
              }
              {screens.lg &&
                (items.map((item) => <Button key={item.content} icon={item.icon} type={item.buttonType} onClick={item.onClick} disabled={isLoading}>{item.content}</Button>))
              }
            </Space>
          </Col>
        )
      }
    </Row>
  )
};

interface ContentProps {
  children: ReactNode;
  isLoading: boolean;
}

export const GcPageContainerContent = (props: ContentProps) => {
  const { isLoading, children } = props;
  return (
    <GcNoDataLoader isLoading={isLoading}>
      {children}
    </GcNoDataLoader>
  )
};

interface DrawerProps {
  children: ReactNode | ReactNode[];
}

export const GcPageContainerDrawers = (props: DrawerProps) => {
  const { children } = props;
  return (<>{children}</>);
};

interface Props {
  children: ReactElement | ReactElement[];
}

const checkComponentName = (child: React.ReactElement<any, string | React.JSXElementConstructor<any>>, elementName: string): boolean => {
  return (child.type as (props: Props) => JSX.Element)?.name === elementName;
}

export const GcPageContainer = (props: Props) => {
  const { children } = props;
  let _content, _header, _drawers;

  Children.forEach(children, child => {
    if (checkComponentName(child, GcPageContainerHeader.name)) {
      return _header = child
    }

    if (checkComponentName(child, GcPageContainerContent.name)) {
      return _content = child
    }

    if (checkComponentName(child, GcPageContainerDrawers.name)) {
      return _drawers = child
    }
  })

  return (
    <Layout style={{ height: '100%' }}>
      <ConfigProvider
        theme={{
          components: {
            Layout: {
              colorBgHeader: purple[8],
            }
          }
        }}
      >
        <Header style={{ paddingInline: 20 }}>
          {_header}
        </Header>
      </ConfigProvider>
      <Content style={{ padding: 12, height: '100%' }}>
        {_content}
        {_drawers}
      </Content>
    </Layout>
  )
}

