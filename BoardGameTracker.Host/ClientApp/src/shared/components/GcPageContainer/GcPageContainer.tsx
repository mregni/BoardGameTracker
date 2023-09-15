import {
  Button, Col, ConfigProvider, Dropdown, Layout, MenuProps, Row, Space, Typography,
} from 'antd';
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

  const dropdownItems: MenuProps['items'] =
    items.map((item, i) => { return { key: i, icon: item.icon, label: <Link to={item.to ?? ''} onClick={item.onClick}>{item.content}</Link> } })

  return (
    <Row justify="space-between" align='middle' style={{ width: '100%' }}>
      <Col>
        <Space wrap>
          {
            hasBack && (
              <Button
                type="text"
                size='large'
                icon={<ArrowLeftOutlined />}
                onClick={() => navigate(backNavigation)}
                disabled={isLoading}
              />
            )
          }
          <Title level={screens.lg ? 3 : 5} style={{ margin: 0, paddingLeft: (hasBack ? 0 :10) }}>{title}</Title>
        </Space>
      </Col>
      {
        screens.lg !== undefined && (
          <Col>
            <Space wrap>
              {!screens.lg &&
                <Dropdown menu={{ items: dropdownItems }} placement="bottomRight" arrow={{ pointAtCenter: true }}>
                  <Button icon={<MoreOutlined />} size='large' type='ghost'></Button>
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
  hasData: boolean;
}

export const GcPageContainerContent = (props: ContentProps) => {
  const { isLoading, children, hasData } = props;
  return (
    <GcNoDataLoader isLoading={isLoading} hasData={hasData}>
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
  const screens = useBreakpoint();

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

  const height = screens.lg ? '64px' : '40px';

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
        <Header style={{ paddingInline: screens.lg ? 20 : 10, height: height, lineHeight: height }}>
          {_header}
        </Header>
      </ConfigProvider>
      <Content style={{ padding: 12 }}>
        {_content}
        {_drawers}
      </Content>
    </Layout>
  )
}

