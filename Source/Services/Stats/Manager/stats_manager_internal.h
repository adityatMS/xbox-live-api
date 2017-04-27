// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once
#include "xsapi/stats_manager.h"
#include "xsapi/system.h"
#include "system_internal.h"
#include "user_context.h"
#include <string>
#include <sstream>
#include <iostream>
#include "xsapi/mem.h"
#include "call_buffer_timer.h"

namespace xbox { namespace services { namespace stats { namespace manager { 

enum class svd_event_type
{
    unknown,
    stat_change,
    stat_delete
};

enum class svd_state
{
    not_loaded,
    loaded,
    offline_not_loaded,
    offline_loaded,
};

struct stat_pending_state
{
    stat_pending_state() :
        statDataType(stat_data_type::undefined)
    {
        initialize_char_arr(statPendingName);
    }

    stat_data_type statDataType;
    char_t statPendingName[STAT_PRESENCE_CHARS_NUM];
    stat_data statPendingData;
};

// stat value document event
class svd_event
{
public:
    svd_event(_In_ stat_pending_state statPendingState, _In_ svd_event_type eventType = svd_event_type::stat_change);

    svd_event_type event_type() const;
    const stat_pending_state& stat_info() const;

private:
    svd_event_type m_svdEventType;
    stat_pending_state m_statPendingState;
};

/// internal class
/// The stat value document holds all of the stat information in an unordered map
class stats_value_document
{
public:
    void set_flush_function(
        _In_ const std::function<void()> flushFunction
        );

    xbox_live_result<stat_value> get_stat(
        _In_ const char_t* name
        ) const;

    xbox_live_result<void> set_stat(
        _In_ const char_t* statName,
        _In_ double statValue
        );

    xbox_live_result<void> set_stat(
        _In_ const char_t* statName,
        _In_ const char_t* statValue
        );

    void get_stat_names(
        _Inout_ std::vector<string_t>& statNameList
        ) const;

    xbox_live_result<void> delete_stat(
        _In_ const char_t* name
        );

    void increment_revision();

    web::json::value serialize();

    uint64_t revision() const;

    bool is_dirty() const;

    void clear_dirty_state();

    void do_work();

    void set_state(_In_ svd_state svdState);

    void merge_stat_value_documents(_In_ const stats_value_document& mergeSVD);

    svd_state state() const;

    stats_value_document();

    static xbox_live_result<stats_value_document> _Deserialize(
        _In_ const web::json::value& data
        );

private:
    bool m_isDirty;
    svd_state m_state;
    uint64_t m_revision;
    std::function<void()> m_fRequestFlush;
    xsapi_internal_string m_clientId;
    xsapi_internal_vector(svd_event) m_svdEventList;
    xsapi_internal_unordered_map(string_t, stat_value) m_statisticDocument;
};

/// internal class
/// The service which handles the http requests to the simplified stats service
class simplified_stats_service
{
public:
    simplified_stats_service();

    simplified_stats_service(
        _In_ std::shared_ptr<xbox::services::user_context> userContext,
        _In_ std::shared_ptr<xbox::services::xbox_live_context_settings> xboxLiveContextSettings,
        _In_ std::shared_ptr<xbox::services::xbox_live_app_config> appConfig
        );

    pplx::task<xbox_live_result<void>> update_stats_value_document(
        _In_ stats_value_document& statValuePostDocument
        );

    pplx::task<xbox_live_result<stats_value_document>> get_stats_value_document();

private:
    string_t pathandquery_simplified_stats_subpath(
        const string_t& xboxUserId,
        const string_t& serviceConfigurationId,
        bool useXuidTag
    ) const;

    std::shared_ptr<xbox::services::user_context> m_userContext;
    std::shared_ptr<xbox::services::xbox_live_context_settings> m_xboxLiveContextSettings;
    std::shared_ptr<xbox::services::xbox_live_app_config> m_appConfig;
};

struct stats_user_context
{
    stats_user_context() {}
    stats_user_context(
        _In_ stats_value_document _statValueDoc,
        _In_ std::shared_ptr<xbox_live_context_impl> _xboxLiveContextImpl,
        _In_ simplified_stats_service _simplifiedStatsService,
        _In_ xbox_live_user_t  _xboxLiveUser
    ) :
        statValueDocument(std::move(_statValueDoc)),
        xboxLiveContextImpl(std::move(_xboxLiveContextImpl)),
        simplifiedStatsService(std::move(_simplifiedStatsService)),
        xboxLiveUser(std::move(_xboxLiveUser))
    {
    }

    stats_value_document statValueDocument;
    std::shared_ptr<xbox_live_context_impl> xboxLiveContextImpl;
    xbox_live_user_t xboxLiveUser;
    simplified_stats_service simplifiedStatsService;
};

class stats_manager_impl : public std::enable_shared_from_this<stats_manager_impl>
{
public:
    stats_manager_impl();

    xbox_live_result<void> add_local_user(
        _In_ const xbox_live_user_t& user
        );

    xbox_live_result<void> remove_local_user(
        _In_ const xbox_live_user_t& user
        );

    xbox_live_result<void> request_flush_to_service(
        _In_ const xbox_live_user_t& user,
        _In_ bool isHighPriority = false
        );

    std::vector<stat_event> do_work();

    xbox_live_result<void> set_stat(
        _In_ const xbox_live_user_t& user,
        _In_ const string_t& name,
        _In_ double value
        );

    xbox_live_result<void> set_stat(
        _In_ const xbox_live_user_t& user,
        _In_ const string_t& name,
        _In_ const char_t* value
        );

    xbox_live_result<void> get_stat_names(
        _In_ const xbox_live_user_t& user,
        _Inout_ std::vector<string_t>& statNameList
        );

    xbox_live_result<stat_value> get_stat(
        _In_ const xbox_live_user_t& user,
        _In_ const string_t& name
        );

    xbox_live_result<void> delete_stat(
        _In_ const xbox_live_user_t& user,
        _In_ const string_t& name
        );

    void initialize();

    xbox_live_result<void> get_leaderboard(
        _In_ const xbox_live_user_t& user,
        _In_ const string_t& statName,
        _In_ leaderboard::leaderboard_query query
    );

    xbox_live_result<void> get_social_leaderboard(
        _In_ const xbox_live_user_t& user,
        _In_ const string_t& statName,
        _In_ const string_t& socialGroup,
        _In_ leaderboard::leaderboard_query query
    );

    void add_leaderboard_result(
        _In_ const xbox_live_user_t& user,
        _In_ const xbox_live_result<leaderboard::leaderboard_result>& result
    );

private:
    static inline bool should_write_offline(xbox_live_result<void>& postResult)
    {
        return postResult.err() == xbox_live_error_condition::network || postResult.err() == xbox_live_error_condition::http_429_too_many_requests 
            || postResult.err() == xbox_live_error_code::http_status_500_internal_server_error || postResult.err() == xbox_live_error_code::http_status_502_bad_gateway
            || postResult.err() == xbox_live_error_code::http_status_503_service_unavailable || postResult.err() == xbox_live_error_code::http_status_504_gateway_timeout;
    }

    void write_offline(
        _In_ stats_user_context& userContext
        );

#if TV_API
    ULONG event_write_stat_update(
        _In_ PCWSTR userId,
        _In_ PCWSTR statDocument
        );
#endif

    void flush_to_service(
        _In_ stats_user_context& statsUserContext
        );

    void update_stats_value_document(_In_ stats_user_context& statsUserContext);

    void request_flush_to_service_callback(_In_ const string_t& userXuid);

    void run_flush_timer();

    static const std::chrono::seconds TIME_PER_CALL_SEC;
    static const std::chrono::milliseconds STATS_POLL_TIME_MS;

#if TV_API
    static std::mutex m_initLock;
    static bool s_bHasInitialized;
    static std::string s_eventProviderName;
    static GUID s_eventPlayerSessionId;
#endif

    std::vector<stat_event> m_statEventList;
    std::vector<xbox::services::xbox_live_result<leaderboard::leaderboard_result>> m_leaderboardResults;
    std::unordered_map<string_t, stats_user_context> m_users;
    std::shared_ptr<xbox::services::call_buffer_timer> m_statTimer;
    std::shared_ptr<xbox::services::call_buffer_timer> m_statPriorityTimer;
    // TODO: change back to xsapi_internal_string
    std::mutex m_statsServiceMutex;
};

}}}}