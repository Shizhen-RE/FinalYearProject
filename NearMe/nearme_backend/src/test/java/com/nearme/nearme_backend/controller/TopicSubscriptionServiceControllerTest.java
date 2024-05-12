package com.nearme.nearme_backend.controller;

import static org.mockito.ArgumentMatchers.anyFloat;
import static org.mockito.ArgumentMatchers.anyInt;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.ArgumentMatchers.anyString;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

import org.junit.Test;
import org.junit.jupiter.api.BeforeAll;
import org.mockito.Mockito;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.request.MockMvcRequestBuilders;

import static org.hamcrest.Matchers.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.google.auth.oauth2.GoogleCredentials;
import com.google.firebase.FirebaseApp;
import com.google.firebase.FirebaseOptions;
import com.google.firebase.auth.FirebaseAuth;
import com.nearme.nearme_backend.model.NearMeTopic;
import com.nearme.nearme_backend.service.TopicSubscriptionService;
import com.nearme.nearme_backend.testutils.TopicEntry;
import com.nearme.nearme_backend.testutils.UserEntry;
import com.nearme.nearme_backend.testutils.utilFuncs;

@WebMvcTest(TopicSubscriptionServiceController.class)
public class TopicSubscriptionServiceControllerTest {
    @Autowired
    MockMvc mockMvc;
    @Autowired
    ObjectMapper mapper;

    @MockBean
    TopicSubscriptionService service;

    private List<UserEntry> users;
    private List<TopicEntry> topics;

    @BeforeAll
    public void testInit() {
        users = utilFuncs.initUsers();
    }
    
    @Test
    public void getTopicList_test() throws Exception {
        Mockito.when(FirebaseAuth.getInstance().verifyIdToken(anyString()).getUid()).thenReturn(anyString());
        List<NearMeTopic> respList = new ArrayList<NearMeTopic>();
        respList.add(new NearMeTopic(anyString(), anyString(),anyString(), anyLong(), anyString(), anyInt(), anyInt()));
        respList.add(new NearMeTopic(anyString(), anyString(),anyString(), anyLong(), anyString(), anyInt(), anyInt()));
        respList.add(new NearMeTopic(anyString(), anyString(),anyString(), anyLong(), anyString(), anyInt(), anyInt()));
        Mockito.when(service.getTopicList(anyString(), anyString())).thenReturn(respList);
        mockMvc.perform(MockMvcRequestBuilders
                .get("/getTopicList")
                .param("lat", "50.34452")
                .param("lng", "-50.255")
                .accept(MediaType.APPLICATION_JSON)
                .header("Authorization", "Bearer mockAuth"))
            .andExpect(status().isOk())
            .andExpect(jsonPath("$", hasSize(3)));
    }
}
